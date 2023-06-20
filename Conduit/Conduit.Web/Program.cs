using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Conduit.Web.Models;
using Conduit.Web.Users.Handlers;
using Conduit.Web.Users.Services;
using Couchbase.Extensions.DependencyInjection;
using FluentValidation;
using Microsoft.OpenApi.Models;

namespace Conduit.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });

                // Configure JWT authentication
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "JWT Authorization header using the Bearer scheme",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                };

                c.AddSecurityDefinition("Token", securityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Token"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "ConduitAspNetCouchbase_Issuer", // Replace with your issuer
                        ValidAudience = "ConduitAspNetCouchbase_Audience", // Replace with your audience
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("6B{DqP5aT,3b&!YRgk29m@j$L7uvnxE")) // Replace with your secret key
                    };
                    options.Events = new JwtBearerEvents()
                    {
                        OnMessageReceived = ctx =>
                        {
                            if (ctx.Request.Headers.ContainsKey("Authorization"))
                            {
                                var bearerToken = ctx.Request.Headers["Authorization"].ElementAt(0);
                                var token = bearerToken.StartsWith("Token ") ? bearerToken.Substring(6) : bearerToken;
                                ctx.Token = token;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
            builder.Services.AddTransient(typeof(SharedUserValidator<>));
            builder.Services.AddTransient<IAuthService, AuthService>();
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
            builder.Services.AddCouchbase(builder.Configuration.GetSection("Couchbase"));
            builder.Services.AddCouchbaseBucket<IConduitBucketProvider>(builder.Configuration["Couchbase:BucketName"], b =>
            {
                b
                    .AddScope(builder.Configuration["Couchbase:ScopeName"])
                    .AddCollection<IConduitUsersCollectionProvider>(builder.Configuration["Couchbase:UsersCollectionName"]);
            });

            // ****************************************************

            var app = builder.Build();

            app.UseAuthentication();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();

            // Add the following line to close the Couchbase connection inside the app.Run() method at the end of Program.cs
            app.Lifetime.ApplicationStopped.Register(() =>
            {
                app.Services.GetRequiredService<ICouchbaseLifetimeService>().Close();
            });
        }
    }
}
