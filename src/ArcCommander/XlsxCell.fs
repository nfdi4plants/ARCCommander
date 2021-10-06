module XlsxCell

open FSharpSpreadsheetML
open System.Collections.Generic

/// The color of a text, a frame, or a cell.
type Color = {
    /// The values for red, blue and green.
    RGB     : int * int * int
    /// The name of this color, if present.
    Name    : string option
}

/// The type of format of a text.
type TextFormatType =
| Bold
| Italic
| Underlined
| Size
| Color of Color
| Font

/// The format of a text.
type TextFormat = {
    /// The formatted positions of the text.
    Positions   : seq<int>
    /// The type of format of a text.
    FormatType  : TextFormatType
}

/// The type of a line in a frame.
type LineType =
| No
| Line
| LineThick
| LineThickPlus
| LineDoubled
| Dotted
| DashedSmall
| DashedLong
| DashedThick
| LongSmallAlternatingDashed
| LongSmallAlternatingDashedThick
| LongSmallSmallAlternatingDashed
| LongSmallSmallAlternatingDashedThick
| LongSmallAlternatingCutDashedThick

/// The line in a frame.
type FrameLine = {
    /// The type of a line in a frame.
    LineType    : LineType
    /// The color of a text, a frame, or a cell.
    Color       : Color
}

/// The frame of a cell.
type Frame = {
    LeftBorder                              : FrameLine
    RightBorder                             : FrameLine
    TopBorder                               : FrameLine
    BottomBorder                            : FrameLine
    DiagonalCrossingLeftTopToRightBottom    : FrameLine
    DiagonalCrossingLeftBottomToRightTop    : FrameLine
}

/// The depiction of negative numbers.
type NegativeNumbers =
/// Only a minus before a negative number.
| MinusBlack
/// No minus but a red color of a negative number.
| Red
/// A minus before a negative number and a black space after it.
| MinusBlackSpace
/// A minus before a negative number, number is in red color, and there is a black space after it.
| MinusRedSpace

/// The number cell category.
type Number = {
    /// The number of digits after the decimal separator.
    DecimalDigits       : int
    /// Show thousand separators or not.
    ThousandSeparators  : bool
    /// The depiction type of negative numbers.
    NegativeNumbers     : NegativeNumbers
}

/// The currency symbol used.
type CurrencySymbol =
| None
| Euro
| Dollar
/// All currently not implemented CurrencySymbols.
| Other of string

/// The currency cell category.
type Currency = {
    /// The number of digits after the decimal separator.
    DecimalDigits       : int
    /// The currency symbol used.
    CurrencySymbol      : CurrencySymbol
    /// The depiction type of negative numbers.
    NegativeNumbers     : NegativeNumbers
}

/// The accounting cell category.
type Accounting = {
    /// The number of digits after the decimal separator.
    DecimalDigits       : int
    /// The currency symbol used.
    CurrencySymbol      : CurrencySymbol
}

/// The depiction type of date cell category.
type Date =
| German1
/// All currently not implemented types of Date.
| Other of string

/// The depiction type of time cell category.
type Time =
| German1
/// All currently not implemented types of Time.
| Other of string

/// The depiction of percent cell category.
type Percent = {
    /// The number of digits after the decimal separator.
    DecimalDigits       : int
}

/// The depiction type of fraction cell category.
type Fraction =
| Monadic
| Binary
| ThreeDigit
| Halves
| Quarters
| Eighth
| Sixteenth
| Tenth
| Hundredth

/// The depiction of scientific cell category.
type Scientific = {
    /// The number of digits after the decimal separator.
    DecimalDigits       : int
}

/// The depiction type of special format cell category.
type SpecialFormat =
| PostcodeGermanStandard
/// All currently not implemented types of SpecialFormat.
| Other of string

/// The depiction type of user-specific cell category.
type UserSpecific =
| Standard
/// All currently not implemented suggestion types of UserSpecific.
| Other of string
/// Custom user-specific depiction.
| Custom of string

/// The category a cell is assigned to.
type CellCategory =
| Standard
| Number of Number
| Currency of Currency
| Accounting of Accounting
| Date of Date
| Time of Time
| Percent of Percent
| Fraction of Fraction
| Scientific of Scientific
| Text
| SpecialFormat of SpecialFormat
| UserSpecific of UserSpecific

/// The horizontal alignment format for text.
type HorizontalTextAlignment =
| Standard
| Left
| Centered
| Right
| FillOut
| Blocktext
| CenteredOverSelection
| Distributed

/// The vertical alignment format for text.
type VerticalTextAlignment =
| Top
| Centered
| Bottom
| Blocktext
| Distributed

/// The direction in which the text is built up.
type TextDirection =
| Context
| FromLeftToRight
| FromRightToLeft

/// The orientation of text in a cell.
type CellOrientation = {
    /// The text orientation of a cell in degrees.
    Orientation             : int
    /// The horizontal alignment format for text.
    HorizontalTextAlignment : HorizontalTextAlignment
    /// The vertical alignment format for text.
    VerticalTexteAlignment  : VerticalTextAlignment
    /// The amount of text indentation.
    Indentation             : int
    /// If word wrap is applied when text is broader than the column width or not.
    WordWrap                : bool
    /// If the text is fit to the cell size or not.
    FitToCellSize           : bool
    /// If cells are merged or not.
    MergeCells              : bool
    /// The direction in which the text is built up.
    LeftToRight             : TextDirection
}

/// The format of a cell.
type CellFormat = {
    /// The category a cell is assigned to.
    CellCategory    : CellCategory
    /// The color of a a cell.
    CellColor       : Color
    /// The frame of a cell.
    CellFrame       : Frame
    /// The orientation of text in a cell.
    CellOrientation : CellOrientation
}

/// A note is the old version of an MS Excel comment. Compared to the new comment, it features text formatting but does not support reply function.
type Note = {
    /// The text written in a note.
    TextContent : string
    /// The formatting of the text content of a note.
    TextFormat  : seq<TextFormat>
    /// The author of a note.
    Author      : string
}

/// A reply is a nested answer to a comment.
type Reply = {
    /// The text written in a comment.
    TextContent : string
    /// The author of a comment.
    Author      : string
}

/// The current version of the MS Excel comment. Compared to notes (the old version of the MS Excel comment), it does not feature text formatting but does support replying.
type Comment = {
    /// The text written in a comment.
    TextContent : string
    /// The author of a comment.
    Author      : string
    /// Replies to a comment.
    Replies     : seq<Reply>
}

// Position (or/also: Reference) is provided via key in tuble or dictionary representation
/// The model representation of an Xlsx cell.
type Cell = {
    /// The written content of a cell.
    Content     : string option
    /// The formatting of the text content of a cell.
    TextFormat  : seq<TextFormat> option
    /// The cell format.
    CellFormat  : CellFormat option
    /// A comment (and associated replies) attached to a cell.
    Comment     : Comment option
    /// A note attached to a cell.
    Note        : Note option
    /// The formula that creates the content of a cell.
    Formula     : string option
}



/// Takes a row index, a column index, a SharedStringTable and the SheetData to give a Cell.
let getCell rowI colI sst sheetData =
    let content = SheetData.tryGetCellValueAt sst rowI colI sheetData
    {
        Content     = content
        TextFormat  = Option<_>.None
        CellFormat  = Option<_>.None
        Comment     = Option<_>.None
        Note        = Option<_>.None
        Formula     = Option<_>.None
    }

/// Creates a sparse Cell matrix from a SharedStringTable and the SheetData. Values are stored sparsely in a dictionary, with the key being a column index and row index tuple.
let getCellMatrix sst sheetData =
    let sm = SheetData.toSparseValueMatrix sst sheetData
    let dict = Dictionary<int * int, Cell>()
    for k in sm do 
        let rowI = fst k.Key |> uint
        let colI = snd k.Key |> uint
        dict.[k.Key] <- getCell rowI colI (Some sst) sheetData
    dict