namespace ChessChallenge.MyBot.Neural_Network;

public interface IActivation
{
    public double Activate(double[] input, int index);
    
    double Derivative(double[] inputs, int index);
}