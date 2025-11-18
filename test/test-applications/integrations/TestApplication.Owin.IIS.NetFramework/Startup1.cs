using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(TestApplication.Owin.IIS.Startup1))]

namespace TestApplication.Owin.IIS;

public class Startup1
{
    public void Configuration(IAppBuilder app)
    {
        app.Map("/healthz", builder => builder.Run(context =>
        {
            context.Response.StatusCode = 200;
            return Task.CompletedTask;
        }));

        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
        app.Map("/test", app1 =>
        {
            app1.Use((context, next) =>
            {
                context.Request.Headers["Custom-Header"] = "CustomValue";
                return next.Invoke();
            });
            app1.Use(async (context, next) =>
            {
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("hello world");
            });
        });
    }
}
