using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix;

internal class TextChunk: PNGChunk {
    private TextKeyword _keyword = TextKeyword.COMMENT;
    private string _lang = string.Empty;

    private string _text = string.Empty;
    private bool _compress = false;

    public string Text { get => _text; set => _text = value; }

    public string Language { get => _lang; set => _lang = value; }

    public TextChunk(TextKeyword keyword, string text, bool compress = false):
            base(name: "iTXt", buffer: UMem<u8>.Invalid) {

        this._keyword = keyword;
        this._text = text;
        this._compress = compress;
    }

    public override void CopyTo(BinaryWriter destination) {
        Span<u8> utf8Text = stackalloc u8[4096];
        Span<u8> headerInfos = stackalloc u8[106];

        /* Keyword as UTF8 bytes */
        if(!Encoding.UTF8.TryGetBytes(chars: Enum.GetName<TextKeyword>(_keyword), bytes: headerInfos[..80], out i32 written))
            throw new ArgumentException(message: "The keyword of the iTXt chunk is too long. (Max: 4096 byte(s))");

        /* Compress indicator */
        headerInfos[81] = _compress ? (u8)1u : (u8)0u;

        /* Language of the text. (83 - 103 bytes) */
        if(_lang != string.Empty)
            _ = Encoding.UTF8.TryGetBytes(chars: _lang, bytes: headerInfos[83..103], out written);

        if(!Encoding.UTF8.TryGetBytes(chars: _text, bytes: utf8Text, out written))
            throw new ArgumentException(message: "The text of the iTXt chunk is too long. (Max: 4096 byte(s))");

        this._buffer = UMem<u8>.Create(allocationLength: 106 + (u32)written, @default: 0);

        headerInfos.CopyTo(destination: _buffer.AsSpan(from: 0, length: 106));
        utf8Text[..written].CopyTo(destination: _buffer.AsSpan(106, length: written));

        base.CopyTo(destination);
    }
}

public enum TextKeyword: u8 {
    COMMENT,
    TITLE,
    SOFTWARE
}
