using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Legato.Interop.AimpRemote;
using Legato.Interop.AimpRemote.Entities;
using Legato.Interop.AimpRemote.Enum;
using System.Diagnostics;

namespace Legato
{
	public class Legato : IDisposable
	{
		public Legato(int pollingIntervalMilliseconds = 100, bool isAutoSubscribing = true)
		{
			_Initialize(pollingIntervalMilliseconds, isAutoSubscribing);
		}

		public Legato(TimeSpan pollingInterval, bool isAutoSubscribing = true)
		{
			if (pollingInterval.TotalMilliseconds > int.MaxValue)
				throw new ArgumentOutOfRangeException("pollingInterval");

			_Initialize((int)pollingInterval.TotalMilliseconds, isAutoSubscribing);
		}

		#region Events

		/// <summary>
		/// AIMP の通知を購読した時に発生します
		/// </summary>
		public event Action Subscribed;

		/// <summary>
		/// AIMP の通知を購読解除した時(または AIMP が終了した時)に発生します
		/// </summary>
		public event Action Unsubscribed;

		#endregion Events

		#region Properties

		public CommunicationWindow Communicator { get; set; }

		/// <summary>
		/// ポーリングの間隔を取得または設定します
		/// <para>この値が大きいと、ポーリングを伴う処理が遅延する可能性があります</para>
		/// </summary>
		public int PollingIntervalMilliseconds { get; set; }

		private System.Timers.Timer _Polling { get; set; }

		/// <summary>
		/// AIMP のイベント通知の購読を自動的に行うかどうかを示す値を取得または設定します
		/// </summary>
		public bool IsAutoSubscribing { get; set; }

		/// <summary>
		/// AIMP のイベント通知を購読しているかどうか(受信可能であるかどうか)を示す値を取得します
		/// </summary>	
		public bool IsSubscribed { get; private set; } = false;

		/// <summary>
		/// AIMP が起動しているかどうかを示す値を取得します
		/// </summary>
		public bool IsRunning => Helper.AimpRemoteWindowHandle != IntPtr.Zero;

		/// <summary>
		/// AIMP の実行ファイルのパスを取得します
		/// </summary>
		public string AimpProcessPath
		{
			get
			{
				var processPath = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Clients\Media\AIMP\shell\open\command")?.GetValue(null)?.ToString();
				if (processPath == null)
					throw new ApplicationException();

				var fileInfo = new FileInfo(processPath);
				if (!fileInfo.Exists)
					throw new FileNotFoundException("AIMP4.exeが見つかりませんでした。ファイルが移動したか、削除された可能性があります");

				return processPath;
			}
		}

		/// <summary>
		/// AIMP を起動します
		/// </summary>
		public Process StartAimp() => Process.Start(AimpProcessPath);

		/// <summary>
		/// AIMP の再生状態を示す値を取得します
		/// </summary>
		public PlayerState State => (PlayerState)Helper.SendPropertyMessage(PlayerProperty.State, PropertyAccessMode.Get).ToInt32();

		/// <summary>
		/// 曲の再生位置を取得または設定します(単位は[ms]です)
		/// </summary>
		public int Position
		{
			get { return Helper.SendPropertyMessage(PlayerProperty.Position, PropertyAccessMode.Get).ToInt32(); }
			set { Helper.SendPropertyMessage(PlayerProperty.Position, PropertyAccessMode.Set, new IntPtr(value)); }
		}

		/// <summary>
		/// 曲の再生音量を取得または設定します(単位は[ms]です)
		/// </summary>
		public int Volume
		{
			get { return Helper.SendPropertyMessage(PlayerProperty.Volume, PropertyAccessMode.Get).ToInt32(); }
			set
			{
				var volume = Math.Max(0, Math.Min(value, 100));
				var result = Helper.SendPropertyMessage(PlayerProperty.Volume, PropertyAccessMode.Set, new IntPtr(volume));

				if (result == IntPtr.Zero)
					throw new ApplicationException("AimpのVolumeプロパティの設定に失敗しました。");
			}
		}

		/// <summary>
		/// ミュート状態であるかどうかを示す値を取得または設定します
		/// </summary>
		public bool IsMute
		{
			get { return Helper.SendPropertyMessage(PlayerProperty.IsMute, PropertyAccessMode.Get) != IntPtr.Zero; }
			set { Helper.SendPropertyMessage(PlayerProperty.IsMute, PropertyAccessMode.Set, new IntPtr(value ? 1 : 0)); }
		}

		/// <summary>
		/// 一曲リピート再生中であるかどうかを示す値を取得または設定します
		/// </summary>
		public bool IsRepeat
		{
			get { return Helper.SendPropertyMessage(PlayerProperty.IsRepeat, PropertyAccessMode.Get) != IntPtr.Zero; }
			set { Helper.SendPropertyMessage(PlayerProperty.IsRepeat, PropertyAccessMode.Set, new IntPtr(value ? 1 : 0)); }
		}

		/// <summary>
		/// ランダム再生中であるかどうかを示す値を取得または設定します
		/// </summary>
		public bool IsShuffle
		{
			get { return Helper.SendPropertyMessage(PlayerProperty.IsShuffle, PropertyAccessMode.Get) != IntPtr.Zero; }
			set { Helper.SendPropertyMessage(PlayerProperty.IsShuffle, PropertyAccessMode.Set, new IntPtr(value ? 1 : 0)); }
		}

		/// <summary>
		/// 再生中の曲の情報を取得します
		/// </summary>
		public TrackInfo CurrentTrack => Helper.CurrentTrack;

		/// <summary>
		/// 再生中のアルバムアートを取得します
		/// </summary>
		public Image AlbumArt
		{
			get
			{
				if (!IsRunning)
					throw new ApplicationException("AlbumArtの取得に失敗しました。AIMPが起動されているかを確認してください。");

				if (!Helper.RequestAlbumArt(Communicator))
					return null;

				Image resource;
				using (var memory = new MemoryStream())
				{
					memory.Write(_AlbumArtSource, 0, _AlbumArtSource.Length);
					resource = Image.FromStream(memory);
				}

				return resource;
			}
		}
		private byte[] _AlbumArtSource { get; set; }

		#endregion Properties

		#region Methods

		private void _Initialize(int pollingIntervalMilliseconds, bool isAutoSubscribing)
		{
			Communicator = new CommunicationWindow();
			_Polling = new System.Timers.Timer(pollingIntervalMilliseconds);
			IsAutoSubscribing = isAutoSubscribing;

			Communicator.CopyDataMessageReceived += (copyData) =>
			{
				// AlbumArtの更新
				if (copyData.dwData == new IntPtr(Helper.CopyDataIdArtWork))
				{
					var dataLength = (int)copyData.cbData;
					_AlbumArtSource = new byte[dataLength];
					Marshal.Copy(copyData.lpData, _AlbumArtSource, 0, dataLength);
				}
			};

			// ポーリング
			_Polling.Elapsed += (s, e) =>
			{
				// 通知が購読されていない
				if (!IsSubscribed)
				{
					if (IsRunning && IsAutoSubscribing)
					{
						// 通知を購読
						Subscribe();
					}
				}

				// 通知が購読されている
				else
				{
					if (IsRunning)
					{
						// PositionProperty
						Communicator.Invoke((Action)(() =>
						{
							Communicator.OnPositionPropertyChanged(Position);
						}));
					}
					else
					{
						// AIMPが終了した
						IsSubscribed = false;
						Unsubscribed?.Invoke();
					}
				}
			};

			_Polling.Start();
		}

		/// <summary>
		/// AIMP のイベント通知を購読します
		/// </summary>
		public void Subscribe()
		{
			if (IsSubscribed)
				throw new ApplicationException("既に通知を購読しています");

			if (!IsRunning)
				throw new ApplicationException("AIMPが起動していないため、購読に失敗しました");

			IsSubscribed = true;
			Helper.RegisterNotify(Communicator);
			Subscribed?.Invoke();
		}

		/// <summary>
		/// AIMP のイベント通知の購読を解除します
		/// </summary>
		public void Unsubscribe()
		{
			if (!IsSubscribed)
				throw new ApplicationException("通知を購読していません");

			IsSubscribed = false;
			Helper.UnregisterNotify(Communicator);
			Unsubscribed?.Invoke();
		}

		public void Dispose()
		{
			_Polling.Stop();

			// 通知の購読解除
			if (IsSubscribed)
				Unsubscribe();
		}

		#region AIMPCommands

		/// <summary>
		/// 曲を再生します
		/// </summary>
		public void Play() => Helper.SendCommandMessage(CommandType.Playing);

		/// <summary>
		/// 再生中の曲の再生状態を切り替えます
		/// </summary>
		public void PlayPause() => Helper.SendCommandMessage(CommandType.PlayPause);

		/// <summary>
		/// 再生中の曲を一時停止します
		/// </summary>
		public void Pause() => Helper.SendCommandMessage(CommandType.Pausing);

		/// <summary>
		/// 再生中の曲を停止します
		/// </summary>
		public void Stop() => Helper.SendCommandMessage(CommandType.Stopped);

		/// <summary>
		/// 次の曲へ移動します
		/// </summary>
		public void Next() => Helper.SendCommandMessage(CommandType.Next);

		/// <summary>
		/// 前の曲へ移動します
		/// </summary>
		public void Prev() => Helper.SendCommandMessage(CommandType.Previous);

		/// <summary>
		/// AIMP を終了します
		/// </summary>
		public void Close() => Helper.SendCommandMessage(CommandType.Quit);

		#endregion AIMPCommands

		#endregion Methods
	}
}
