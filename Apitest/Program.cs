using System.Security.Claims;
using System.Threading.RateLimiting;
using Apitest.Authorization;
using Apitest.Common;
using Apitest.Controllers;
using Apitest.Mapping;
using Apitest.Models;
using Apitest.Repositories;
using Apitest.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Apitest;

public class Program
{
    public static async Task Main(string[] args)
    {
        var initializeAuth = args.Contains("--init-auth", StringComparer.Ordinal);
        var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--init-auth").ToArray());
        builder.Services.AddControllers(options => options.Filters.Add<ServiceExceptionFilter>());
        builder.Services.AddDbContext<ShopBdContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("con")));
        builder.Services.AddScoped<IRoleRepository, RoleRepository>();
        builder.Services.AddScoped<IRoleService, RoleService>();
        builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IProductRepository, ProductRepository>();
        builder.Services.AddScoped<IProductService, ProductService>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
        builder.Services.AddScoped<IOrderItemService, OrderItemService>();
        builder.Services.AddScoped<ICartRepository, CartRepository>();
        builder.Services.AddScoped<ICartService, CartService>();
        builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();
        builder.Services.AddScoped<ICartItemService, CartItemService>();
        builder.Services.AddScoped<IProductReservationRepository, ProductReservationRepository>();
        builder.Services.AddScoped<IProductReservationService, ProductReservationService>();
        builder.Services.AddSingleton(provider => new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
            cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
        }, provider.GetRequiredService<ILoggerFactory>()));
        builder.Services.AddScoped<IMapper>(provider =>
            provider.GetRequiredService<MapperConfiguration>().CreateMapper(provider.GetService));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Вставьте access token."
            });
            options.OperationFilter<AuthorizeOperationFilter>();
        });

        builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        builder.Services.AddScoped<TokenService>();

        if (!initializeAuth)
        {
            var jwt = builder.Configuration.GetSection("Jwt");
            var signingKey = TokenService.GetSigningKey(jwt);
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwt["Issuer"],
                        ValidAudience = jwt["Audience"],
                        IssuerSigningKey = signingKey,
                        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                        RoleClaimType = ClaimTypes.Role,
                        NameClaimType = ClaimTypes.Name,
                        ClockSkew = TimeSpan.Zero
                    };
                });
        }

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AccessRules.AdminOnly, policy => policy.RequireRole(AccessRules.Admin));
            options.AddPolicy(AccessRules.ReservationOwner, policy => policy
                .RequireAuthenticatedUser().AddRequirements(new ReservationOwnerRequirement()));
        });
        builder.Services.AddSingleton<IAuthorizationHandler, ReservationOwnerHandler>();

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        builder.Services.AddCors(options => options.AddPolicy(AccessRules.Frontend, policy => policy
            .WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AccessRules.Login, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 5,
                    QueueLimit = 0
                }));
        });

        await using var app = builder.Build();
        if (initializeAuth)
        {
            await using var scope = app.Services.CreateAsyncScope();
            await AuthSetup.InitializeAsync(scope.ServiceProvider, app.Configuration);
            Console.WriteLine("Роли авторизации настроены.");
            return;
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors(AccessRules.Frontend);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapControllers();
        await app.RunAsync();
    }
}
