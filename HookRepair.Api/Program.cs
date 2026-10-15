using HookRepair.Api.Security;
using HookRepair.Modules.Auth.Application.Auth;
using HookRepair.Modules.Auth.Application.Organizations;
using HookRepair.Modules.Auth.Application.RefreshTokens;
using HookRepair.Modules.Auth.Application.Roles;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Application.Users;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Auth.Infrastructure;
using HookRepair.Modules.Auth.Infrastructure.Auth;
using HookRepair.Modules.Auth.Infrastructure.Organizations;
using HookRepair.Modules.Auth.Infrastructure.RefreshTokens;
using HookRepair.Modules.Auth.Infrastructure.Roles;
using HookRepair.Modules.Auth.Infrastructure.Security;
using HookRepair.Modules.Auth.Infrastructure.Users;
using HookRepair.Modules.Catalogs.Application.Catalogs;
using HookRepair.Modules.Catalogs.Infrastructure.Catalogs;
using HookRepair.Modules.Catalogs.Infrastructure;
using HookRepair.Modules.Crm.Application.Clients;
using HookRepair.Modules.Crm.Application.Suppliers;
using HookRepair.Modules.Crm.Infrastructure.Clients;
using HookRepair.Modules.Crm.Infrastructure;
using HookRepair.Modules.Crm.Infrastructure.Suppliers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using HookRepair.Modules.Inventory.Infrastructure;
using HookRepair.Modules.Inventory.Application.Movements;
using HookRepair.Modules.Inventory.Infrastructure.Movements;
using HookRepair.Modules.Inventory.Application.Products;
using HookRepair.Modules.Inventory.Infrastructure.Products;
using HookRepair.Modules.Inventory.Application.PurchaseOrders;
using HookRepair.Modules.Inventory.Infrastructure.PurchaseOrders;
using HookRepair.Modules.Inventory.Application.GoodsReceipts;
using HookRepair.Modules.Inventory.Infrastructure.GoodsReceipts;
using HookRepair.Modules.Catalogs.Application.Services;
using HookRepair.Modules.Catalogs.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
    ?? throw new InvalidOperationException("Auth configuration is required.");
authOptions.Validate();

builder.Services.AddSingleton<IAuthOptions>(authOptions);
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IPasswordService, BCryptPasswordService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IDefaultRoleService, DefaultRoleService>();
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IModelService, ModelService>();
builder.Services.AddScoped<IClientService, CustomerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IInventoryMovementService, InventoryMovementService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
builder.Services.AddScoped<IServiceService, ServiceService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

// Railway (y cualquier proxy inverso) termina el HTTPS y reenvia HTTP a la app.
// Esto hace que ASP.NET use el esquema y la IP originales del cliente.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// db contexts para cada modulo aqi
builder.Services.AddDbContext<AuthDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseSnakeCaseNamingConvention());
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseSnakeCaseNamingConvention());
builder.Services.AddDbContext<CrmDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseSnakeCaseNamingConvention());
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey)),
            ValidateIssuer = true,
            ValidIssuer = authOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = authOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var tenant = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();
                var rawOrganizationId = context.Principal?.FindFirstValue("org_id");
                if (!Guid.TryParse(rawOrganizationId, out var organizationId) || organizationId == Guid.Empty)
                {
                    context.Fail("Organization claim is required.");
                    return Task.CompletedTask;
                }

                var isPlatform = context.Principal?.FindFirstValue("scope") == "platform";
                tenant.Set(organizationId, isPlatform);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in SystemPermissions.ValidPermissions)
    {
        options.AddPolicy(permission, policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("permission", permission));
    }
});
builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
    .WithOrigins(authOptions.AllowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));
    options.AddPolicy("auth-refresh", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));
});

builder.Services.AddScoped<AuthSeeder>();

var app = builder.Build();

// Debe ser el primer middleware del pipeline
app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;

    // Crea/actualiza las tablas de cada modulo antes de sembrar datos
    await sp.GetRequiredService<AuthDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<CrmDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();

    var seeder = sp.GetRequiredService<AuthSeeder>();
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();
app.UseRouting();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;