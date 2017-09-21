using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Console;

namespace Legato.Sample
{
	public partial class Form1 : Form
	{
		#region Field

		private System.Timers.Timer _Timer;
		private int _TimerCounter;
		private int _MinuteCounter;
		private int _SecondCounter;
		private bool _TimerInitialized;
		private Legato _Legato { get; set; }

		#endregion Field

		#region Constants

		private readonly int _MsConvertSec = 1000;
		private readonly int _BetweenMin = 59;

		#endregion Constants

		public Form1()
		{
			InitializeComponent();

			Icon = Properties.Resources.legato;
			pictureBox1.Image = Properties.Resources.logo;

			_Legato = new Legato();
			_Timer = new System.Timers.Timer();
			_TimerCounter = 0;
			_MinuteCounter = 0;
			_SecondCounter = 0;
			_TimerInitialized = false;

			_Legato.PropertyNotify += (type) =>
			{
				Debug.WriteLine($"プロパティ変更通知: {type}");

				if (type == Interop.AimpRemote.Enum.PropertyType.State)
				{
					pictureBox1.Image = _Legato.AlbumArt ?? Properties.Resources.logo;
				}
			};

			_Legato.TrackInfoNotify += () =>
			{
				Debug.WriteLine($"現在のトラック情報が変更されました");
			};

			_Legato.TrackStartNotify += () =>
			{
				Debug.WriteLine($"トラックがスタートされました");
			};
		}

		private void ResetCounter()
		{
			_TimerCounter = 0;
			_MinuteCounter = 0;
			_SecondCounter = 0;
		}

		/// <summary>
		/// 再生時間の表示を更新します(UIスレッドで実行されます)
		/// </summary>
		private void _UpdateCurrentPos()
		{
			CurrentPos.Invoke((Action)(() =>
			{
				CurrentPos.Text = $"Duration = {_MinuteCounter:D2} : {_SecondCounter:D2}";
			}));
		}

		#region Procedures

		private void buttonFetch_Click(object sender, EventArgs e)
		{
			WriteLine($"IsRunning:{_Legato.IsRunning}");

			if (_Legato?.IsRunning ?? false)
			{
				WriteLine($"State:{_Legato.State}");
				WriteLine($"IsShuffle:{_Legato.IsShuffle}");
				WriteLine($"Volume:{_Legato.Volume}");
				WriteLine($"Position:{_Legato.Position} - {_Legato.Duration}");

				var track = _Legato.CurrentTrack;
				WriteLine($"Title:{track.Title}");
				WriteLine($"Artist:{track.Artist}");
				WriteLine($"Album:{track.Album}");

				pictureBox1.Image = _Legato.AlbumArt ?? Properties.Resources.logo;
			}
		}

		private void buttonPlayPause_Click(object sender, EventArgs e)
		{
			if (_Legato?.IsRunning ?? false)
			{
				if (_Legato.State == Interop.AimpRemote.Enum.PlayerState.Playing)
				{
					_Legato.Pause();
					_Timer.Stop();
				}
				else
				{
					Task.Run(async () =>
					{
						_Legato.Play();

						var timeSec = _Legato.Position / _MsConvertSec;
						var customizeMs = _Legato.Position % _MsConvertSec;

						await Task.Delay(customizeMs);
						_TimerCounter = timeSec;

						_UpdateCurrentPos();

						if (!_TimerInitialized)
						{
							_TimerInitialized = true;

							_Timer.Elapsed += (s, v) =>
							{
								++_TimerCounter;

								if (_SecondCounter == _BetweenMin)
								{
									++_MinuteCounter;
									_SecondCounter = 0;
								}
								else
								{
									++_SecondCounter;
								}

								_UpdateCurrentPos();
							};

							_Timer.Interval = _MsConvertSec;
						}

						_Timer.Start();
					});
				}
			}
		}

		private void buttonNext_Click(object sender, EventArgs e)
		{
			if (_Legato?.IsRunning ?? false)
			{
				_Legato.Next();
				ResetCounter();
			}
		}

		private void buttonPrev_Click(object sender, EventArgs e)
		{
			if (_Legato?.IsRunning ?? false)
			{
				_Legato.Prev();
				ResetCounter();
			}
		}

		#endregion Procedures
	}
}
