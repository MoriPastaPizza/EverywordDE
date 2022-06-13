using EverywordDE.Controller;
using Serilog;

namespace EveryWordDE
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			#if DEBUG
				Log.Logger = new LoggerConfiguration()
					.MinimumLevel.Debug()
					.WriteTo.Console()
					.CreateLogger();
			#else
				Log.Logger = new LoggerConfiguration()
					.MinimumLevel.Information()
					.WriteTo.Console()
					.CreateLogger();
			#endif

			Log.Information("Logger created!");
			
			await TwitterController.Init();
			
			await Task.Delay(-1);
		}
	}
}