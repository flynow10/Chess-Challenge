using Raylib_cs;

namespace ChessChallenge.Application
{
    public static class BotBrainCapacityUI
    {
        static readonly Color green = new(17, 212, 73, 255);
        static readonly Color yellow = new(219, 161, 24, 255);
        static readonly Color orange = new(219, 96, 24, 255);
        static readonly Color red = new(219, 9, 9, 255);
        static readonly Color background = new Color(40, 40, 40, 255);

        public static void Draw(string botName, int numTokens, int tokenLimit, int count, int index)
        {

            int screenWidth = Raylib.GetScreenWidth();
            int screenHeight = Raylib.GetScreenHeight();
            int barWidth = screenWidth / count;
            int barOffset = barWidth * index;
            int height = UIHelper.ScaleInt(48);
            int fontSize = UIHelper.ScaleInt(35);
            // Bg
            Raylib.DrawRectangle(barOffset, screenHeight - height, barWidth, height, background);
            // Bar
            double t = (double)numTokens / tokenLimit;

            Color col;
            if (t <= 0.7)
                col = green;
            else if (t <= 0.85)
                col = yellow;
            else if (t <= 1)
                col = orange;
            else
                col = red;
            Raylib.DrawRectangle(barOffset, screenHeight - height, (int)(barWidth * t), height, col);

            var textPos = new System.Numerics.Vector2(barOffset + (float)barWidth / 2, screenHeight - (float)height / 2);
            string text = $"{botName} Capacity: {numTokens}/{tokenLimit}";
            if (numTokens > tokenLimit)
            {
                text += " [LIMIT EXCEEDED]";
            }
            UIHelper.DrawText(text, textPos, fontSize, 1, Color.WHITE, UIHelper.AlignH.Centre);
        }
    }
}