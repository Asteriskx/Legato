﻿using Legato.Interop.Aimp.Enum;
using Legato.Interop.Win32.Enum;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Legato.Interop.Aimp
{
	/// <summary>
	/// AIMPから提供されるリモートウィンドウを操作する
	/// </summary>
	public class RemoteHelper
	{
		public static readonly string RemoteClassName = "AIMP2_RemoteInfo";
		public static readonly int RemoteMapFileSize = 2048;

		/// <summary>
		/// アートワークのCopyDataIdを示します
		/// </summary>
		public static readonly uint CopyDataIdArtWork = 0x41495043;

		public static IntPtr AimpRemoteWindowHandle
		{
			get
			{
				var handle = Win32.API.FindWindow(RemoteClassName, null);

				if (handle == IntPtr.Zero)
					throw new Exception("remote window not found");

				return handle;
			}
		}

		public static MemoryMappedViewStream RemoteMmfStream
		{
			get
			{
				var mmf = MemoryMappedFile.OpenExisting(RemoteClassName, MemoryMappedFileRights.ReadWrite, HandleInheritability.Inheritable);
				return mmf.CreateViewStream(0, RemoteMapFileSize);
			}
		}

		// send Message (base)
		private static IntPtr SendMessageBase(WindowMessage windowMessage, IntPtr param, IntPtr value)
		{
			IntPtr output;
			var result = Win32.API.SendMessageTimeout(AimpRemoteWindowHandle, windowMessage, param, value, SendMessageTimeoutType.NORMAL, 1000, out output);

			if (result == IntPtr.Zero)
			{
				// TODO 例外処理
				throw new Exception("on SendMessageBase");
			}

			return output;
		}

		// send Property Message
		public static IntPtr SendPropertyMessage(PropertyType propertyType, PropertyAccessMode mode, IntPtr value)
		{
			return SendMessageBase((WindowMessage)AimpWindowMessage.Property, new IntPtr((uint)propertyType | (uint)mode), value);
		}

		public static IntPtr SendPropertyMessage(PropertyType propertyType, PropertyAccessMode mode)
		{
			return SendPropertyMessage(propertyType, mode, IntPtr.Zero);
		}

		// send Command Message
		public static IntPtr SendCommandMessage(CommandType commandType, IntPtr value)
		{
			return SendMessageBase((WindowMessage)AimpWindowMessage.Command, new IntPtr((uint)commandType), value);
		}

		public static IntPtr SendCommandMessage(CommandType commandType)
		{
			return SendCommandMessage(commandType, IntPtr.Zero);
		}

		/// <summary>
		/// アートワークの送信をリクエストします
		/// </summary>
		/// <param name="communicationWindow">ArtWorkを受け取る通信ウィンドウ</param>
		public static bool RequestArtWork(CommunicationWindow communicationWindow)
		{
			return SendCommandMessage(CommandType.GetArtwork, communicationWindow.Handle) != IntPtr.Zero;
		}
	}
}
