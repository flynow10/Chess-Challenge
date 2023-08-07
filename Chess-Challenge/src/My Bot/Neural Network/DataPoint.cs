namespace ChessChallenge.MyBot.Neural_Network;

public class DataPoint
{
    public int[] boardRepresentation;
    
    public DataPoint(int[] boardRep)
    {
        boardRepresentation = boardRep;
    }
}