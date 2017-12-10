﻿using System;
using System.Windows.Forms;

namespace Legato.Sample {
	static class Program {
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main() {
			try {
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new Form1());
			}
			catch (Exception ex) {
				MessageBox.Show($"内容:\r\n{ex.Message}\r\n\r\nスタックトレース:\r\n{ex.StackTrace}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}
