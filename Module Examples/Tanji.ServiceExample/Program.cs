using Tangine.Modules;

namespace Tanji.ServiceExample
{
    [Module("Service Example", "Module example showing how to create a valid service for Tanji.")]
    public class Program : TService
    {
        public Program(string[] args)
        { }
        public static void Main(string[] args)
        {
            var app = new Program(args);
            app.Run();
        }

        public void Run()
        { }
    }
}