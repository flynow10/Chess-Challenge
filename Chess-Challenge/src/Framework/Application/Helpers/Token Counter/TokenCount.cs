namespace ChessChallenge.Application;

public struct TokenCount {
    public int total;
    public int debug;
    public TokenCount(int total, int debug)
    {
        this.total = total;
        this.debug = debug;
    }
}