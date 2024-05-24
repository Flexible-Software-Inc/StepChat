using Microsoft.AspNetCore.SignalR;
using StepChat.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection;
using System.Text;
using StepChat.Contexts;
using StepChat.Classes.Provider;
using StepChat.Classes.Auth;
using StepChat.Classes.Configuration;
using StepChat.Services;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

//                              :-= +*****************************************+=
//                          :+%@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@%
//                        -%@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@=
//                      :%@@@@@@#=-:..........................................              
//                     -@@@@@%=
//                    :@@@@@#                                                               
//                    #@@@@%                                                                
//                   :@@@@@-
//                   *@@@@@                                                                 
//                  .@@@@@=
//                  +@@@@@       .--------------------------.          :-:                  
//                  @@@@@+      =@@@@@@@@@@@@@@@@@@@@@@@@@@@@*        #@@@%.                
//                 =@@@@@.      +@@@@@@@@@@@@@@@@@@@@@@@@@@@@#       -@@@@@.                
//                 %@@@@*        .--------------------------:        %@@@@#                 
//                -@@@@@:                                           :@@@@@:                 
//                #@@@@#                                            #@@@@%                  
//               .@@@@@-                                           .@@@@@=
//               *@@@@%                                            *@@@@@                   
//              .@@@@@=                                           .@@@@@+
//              +@@@@@                                            +@@@@@.                   
//              @@@@@+                                           -@@@@@+
//             =@@@@@.                                          +@@@@@#                     
//             %@@@@*                                       .- *@@@@@@*
//            -@@@@@.   =#%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%@@@@@@@@@#:                       
//            *@@@@#   :@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@%+:                         
//            .*%% *.    =#%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%#*+-.                            


namespace StepChat
{
    public abstract class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<MessengerDataDbContext>();

            builder.Services.AddTransient<ITokenService, TokenService>();
            builder.Services.AddTransient<IConfigService>(_ => new ConfigService("appsettings.json"));

            builder.Services.AddDbContext<MessengerDataDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionString")));

            builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddSingleton<EmailSender>();

            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        ValidateLifetime = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
                        ValidateIssuerSigningKey = true
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            // если запрос направлен хабу
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                            {
                                // получаем токен из строки запроса
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddRazorPages();
            builder.Services.AddSignalR();

            builder.Services.AddDataProtection()
            .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
            {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
            });


            //using (var context = new MessengerDataDbContext())
            //{
            //    // Прочитайте изображение из файла или источника данных
            //    byte[] imageData = File.ReadAllBytes("C:\\Users\\salax\\RiderProjects\\StepChat\\wwwroot\\images\\blank-profile-picture.png");

            //    // Создайте объект Image
            //    var image = new Models.ImagesModel { ImageId = 0, Image = imageData };

            //    // Добавьте изображение в контекст данных и сохраните изменения
            //    context.Images.Add(image);
            //    context.SaveChanges();
            //}

            builder.Services.AddSession();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseSession();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Authorization}/{action=LoginPage}/{id?}");
            app.MapHub<ChatHub>("/chatHub");
            app.Run();
        }
    }
}
