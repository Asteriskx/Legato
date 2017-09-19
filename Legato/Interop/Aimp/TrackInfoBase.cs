﻿namespace Legato.Interop.Aimp
{
    /// <summary>
    /// 再生中のトラック情報の基礎定義クラスです。
    /// </summary>
    internal class TrackInfoBase
    {
        /// <summary>
        /// ヘッダサイズを取得・設定します。
        /// </summary>
        public uint HeaderSize { get; set; }

        /// <summary>
        /// マスク値の取得・設定をします。
        /// </summary>
        public uint Mask { get; set; }

        /// <summary>
        /// 現在の再生状況を取得・設定します。
        /// </summary>
        public uint AlbumStringLength { get; set; }

        /// <summary>
        /// 再生中の曲のアーティスト名の長さを取得・設定します。
        /// </summary>
        public uint ArtistStringLength { get; set; }

        /// <summary>
        /// 再生中の曲のリリース年の長さを取得・設定します。
        /// </summary>
        public uint YearStringLength { get; set; }

        /// <summary>
        /// 再生中の曲のファイルパス名の長さを取得・設定します。
        /// </summary>
        public uint FilePathStringLength { get; set; }

        /// <summary>
        /// 再生中の曲のジャンル名の長さを取得・設定します。
        /// </summary>
        public uint GenreStringLength { get; set; }

        /// <summary>
        /// 再生中の曲名の長さを取得・設定します。
        /// </summary>
        public uint TitleStringLength { get; set; }
    }
}
