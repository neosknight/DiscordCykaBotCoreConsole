using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.EventArgs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordCykaBotCoreConsole.Commands
{
    public class TestCommands: BaseCommandModule
    {
        [Command("ping")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Pong").ConfigureAwait(false);
        }

        [Command("sound")]
        public async Task Sound(CommandContext ctx, params string[] args)
        {
			string name = "";
			int repeatCnt = 1;

			if (args.Length > 0)
				name = args[0];

			if (args.Length > 1)
				int.TryParse(args[1], out repeatCnt);

			PlaySound(ctx, name, repeatCnt);
        }

        public async Task PlaySound(CommandContext ctx, string name = "", int repeatCnt = 1)
        {
			if (repeatCnt > 3) return;

			string file = "sounds/" + name + ".mp3";

			var vnext = ctx.Client.GetVoiceNext();

			var vnc = vnext.GetConnection(ctx.Guild);
			if (vnc == null)
				throw new InvalidOperationException("Not connected in this guild.");

			if (!File.Exists(file))
				throw new FileNotFoundException("File was not found.");

			await vnc.SendSpeakingAsync(true); // send a speaking indicator

			for(int i = 0; i < repeatCnt; i++)
			{
				var psi = new ProcessStartInfo
				{
					FileName = "ffmpeg",
					Arguments = $@"-i ""{file}"" -ac 2 -f s16le -ar 48000 pipe:1",
					RedirectStandardOutput = true,
					UseShellExecute = false
				};
				var ffmpeg = Process.Start(psi);
				var ffout = ffmpeg.StandardOutput.BaseStream;

				var txStream = vnc.GetTransmitStream();
				await ffout.CopyToAsync(txStream);
				await txStream.FlushAsync();

				await vnc.WaitForPlaybackFinishAsync(); // wait until playback finishes
			}
		}

		[Command("join")]
		public async Task Join(CommandContext ctx)
		{
			var chn = ctx.Member?.VoiceState?.Channel;

			if (chn == null)
			{
				throw new InvalidOperationException("You need to be in a voice channel.");
			}

			await chn.ConnectAsync();
			
			ctx.Client.GetVoiceNext().GetConnection(ctx.Guild).VoiceReceived += OnVoiceReceived;
		}

		private Task OnVoiceReceived(VoiceReceiveEventArgs e)
		{
			if(e.User.Username == "neosknight")
			{
				ReadOnlyMemory<byte> m = e.PcmData;
				foreach(var b in m.ToArray())
				{
					Console.Write(b);
				}
				Console.WriteLine();

				using (var stream = new FileStream("kek.txt", FileMode.Append))
				{
					stream.Write(m.ToArray(), 0, m.ToArray().Length);
				}
			}

			return Task.CompletedTask;
		}
	}
}
