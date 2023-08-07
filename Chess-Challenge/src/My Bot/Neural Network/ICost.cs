namespace ChessChallenge.MyBot.Neural_Network;

public interface ICost
{
    double CostFunction(double[] predictedOutputs, double[] expectedOutputs);

    double CostDerivative(double predictedOutput, double expectedOutput);

}