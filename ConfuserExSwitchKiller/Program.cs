using System;
using System.Windows.Forms;

namespace ConfuserExSwitchKiller
{
	// Token: 0x020002CE RID: 718
	internal sealed class Program
	{
		// Token: 0x06002157 RID: 8535 RVA: 0x00092EE8 File Offset: 0x00091EE8
		[STAThread]
		private static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
	}
}
