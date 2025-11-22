using System;
using System.Linq;
using ChessChallenge.API;
using ChessChallenge.Application;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ChessChallenge.Testing;

public class PieceSquareTable : TestSuite
{
    record struct PieceTable(int[] squares, PieceType piece, string name);

    private PieceTable[] MyBotTables;
    private PieceTable[] V10Tables;
    
    ulong[] MyBotTablesPacked =
    {
        9913330531774723959, 8609676836631704936, 11078252110869744008, 8608480570021773311,
        250098419548360960, 1715269411402468225, 1710465645101833345, 4803766359914288,
        6374695211575366995, 6312245082029922709, 6307740382873098373, 3843071673468680053,
        7455559058829379447, 7455559058560874358, 7455559058560874358, 8608480568035350936,
        6302638648329659731, 7460362828890278021, 6307441315961931894, 3843370740631435125,
        13508397255544502747, 3535326889997710133, 1152921509170249729, 1152921509170249729,
        1258605535789388048, 1576256919301905233, 1566649386700635473, 5401900951762225
    };
    
    ulong[] V10TablesPacked =
    {
        657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569,
        366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421,
        366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430,
        402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514,
        329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759,
        291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181,
        402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804,
        347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047,
        347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538,
        384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492,
        347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100,
        366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863,
        419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932,
        329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691,
        383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375,
        329978099633296596, 67159620133902
    };
    
    public PieceSquareTable(BotTester botTester) : base(botTester)
    {
        MyBotTables = new PieceTable[7];
        for (int pieceTypeIndex = 0; pieceTypeIndex <= 6; pieceTypeIndex++)
        {
            PieceType pieceType = (PieceType)(pieceTypeIndex + 1);
            PieceTable table = new PieceTable(new int[64], pieceType, pieceType.ToString());
            if (pieceTypeIndex == 6)
            {
                table.piece = PieceType.King;
                table.name = "King";
            }
            for (int square = 0; square < 64; square++)
            {
                table.squares[square] =
                    (int)((MyBotTablesPacked[pieceTypeIndex * 4 + square / 16] >> square % 16 * 4) & 15) * 5 - 35;
            }

            MyBotTables[pieceTypeIndex] = table;
        }

        V10Tables = new PieceTable[12];
        for (int pieceTypeIndex = 0; pieceTypeIndex < 6; pieceTypeIndex++)
        {
            PieceType pieceType = (PieceType)(pieceTypeIndex + 1);
            PieceTable midTable = new PieceTable(new int[64], pieceType, pieceType + " Mid");
            PieceTable endTable = new PieceTable(new int[64], pieceType, pieceType + " End");
            for (int square = 0; square < 64; square++)
            {
                int pstIndex = 128 * (pieceTypeIndex) + square ^ 56;
                midTable.squares[square] = GetV10Score(pstIndex);
                endTable.squares[square] = GetV10Score(pstIndex + 64);
            }
            V10Tables[pieceTypeIndex] = midTable;
            V10Tables[pieceTypeIndex + 6] = endTable;
        }

        int GetV10Score(int index) => (int)(((V10TablesPacked[index / 10] >> (6 * (index % 10))) & 63) - 20) * 8;
    }

    public override string Name { get; } = "Piece Square Table Tester";
    public override SuiteResult RunSuiteGroup(BotReference botType, BotReference baseBot, string groupId)
    {
        string result = "";
        while (result != "Exit")
        {
            switch (botType.Type)
            {
                case PlayerType.MyBotV10:
                {
                    string[] pieces =
                    {
                        "Pawn Mid", 
                        "Knight Mid", 
                        "Bishop Mid", 
                        "Rook Mid", 
                        "Queen Mid", 
                        "King Mid",
                        "Pawn End", 
                        "Knight End", 
                        "Bishop End", 
                        "Rook End", 
                        "Queen End", 
                        "King End",
                    };
                    result = AnsiConsole.Prompt(new SelectionPrompt<string>()
                        .AddChoices(pieces)
                        .AddChoices("Exit")
                    );
                    if (pieces.Contains(result))
                    {
                        int pieceTable = Array.IndexOf(pieces, result);
                        PrintTable(V10Tables[pieceTable]);
                    }

                    break;
                }
                default:
                {
                    string[] pieces = { "Pawn", "Knight", "Bishop", "Rook", "Queen", "King Mid", "King Late" };
                    result = AnsiConsole.Prompt(new SelectionPrompt<string>()
                        .AddChoices(pieces)
                        .AddChoices("Exit")
                    );
                    if (pieces.Contains(result))
                    {
                        int pieceTable = Array.IndexOf(pieces, result);
                        PrintTable(MyBotTables[pieceTable]);
                    }

                    break;
                }
            }
        }
        return new SuiteResult("visualize");
    }

    private void PrintTable(PieceTable pieceTable)
    {
        Table table = new Table();
        table.Centered();
        table.ShowRowSeparators = true;
        table.Title(pieceTable.name + " Piece Table");
        int max = pieceTable.squares.Max();
        int min = pieceTable.squares.Min();
        for (int row = 7; row >= 0; row--)
        {
            IRenderable[] rowStr = new IRenderable[9];
            rowStr[0] = new Markup((row + 1).ToString()).LeftJustified();
            for (int col = 0; col < 8; col++)
            {
                int pieceSquareValue = pieceTable.squares[row * 8 + col];
                int colorValue = (pieceSquareValue - min) * 255 / (max - min);
                rowStr[col + 1] = new Markup($"[default on #{PrintHex(255-colorValue)}00{PrintHex(colorValue)}]{pieceSquareValue}[/]").Centered();
            }

            if (row == 7)
            {
                table.AddColumns(rowStr.Select(renderable => new TableColumn(renderable)).ToArray());
            }
            else
            {
                table.AddRow(rowStr);
            }
        }
        table.AddRow(new string[9].Select((_, i) => new Markup(i == 0 ? "" : Char.ToString((char)('a' + i - 1))).Centered()));
        AnsiConsole.Write(table);
    }

    private string PrintHex(int num)
    {
        return num.ToString("X").PadLeft(2, '0');
    }

    public override SuiteGroup[] GetAllGroups()
    {
        return new[]
        {
            new SuiteGroup("visualize", "Visualize")
        };
    }
}