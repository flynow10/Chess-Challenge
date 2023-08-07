using static System.Math;

namespace ChessChallenge.MyBot.Neural_Network;

public class Layer
{
    public readonly int numInputNodes;
    public readonly int numOutputNodes;
    
    public double[,] weights;
    public double[] biases;
    
    // Cost gradient with respect to weights and with respect to biases
    public readonly double[,] costGradientW;
    public readonly double[] costGradientB;
    
    public readonly double[,] weightVelocities;
    public readonly double[] biasVelocities;

    public IActivation activation;

    public Layer(int inputs, int outputs, System.Random rng)
    {
        numInputNodes = inputs;
        numOutputNodes = outputs;

        weights = new double[inputs, outputs];
        costGradientW = new double[weights.Length, weights.GetLength(1)];
        biases = new double[outputs];
        costGradientB = new double[biases.Length];
        
        weightVelocities = new double[weights.Length, weights.GetLength(1)];
        biasVelocities = new double[biases.Length];
        
        InitializeRandomWeights(rng);
    }

    public double[] CalculateOutputs(double[] inputs)
    {
        double[] weightedInputs = new double[numOutputNodes];

        for (int nodeOut = 0; nodeOut < numOutputNodes; nodeOut++)
        {
            double weightedInput = biases[nodeOut];
            for (int nodeIn = 0; nodeIn < numInputNodes; nodeIn++)
            {
                weightedInput += inputs[nodeIn] * weights[nodeIn, nodeOut];
            }

            weightedInputs[nodeOut] = weightedInput;
        }

        double[] activations = new double[numOutputNodes];
        for (int outputNode = 0; outputNode < numOutputNodes; outputNode++)
        {
            activations[outputNode] = activation.Activate(inputs, outputNode);
        }

        return activations;
    }
    
    public double[] CalculateOutputs(double[] inputs, LayerLearnData learnData)
    {
        learnData.inputs = inputs;

        for (int nodeOut = 0; nodeOut < numOutputNodes; nodeOut++)
        {
            double weightedInput = biases[nodeOut];
            for (int nodeIn = 0; nodeIn < numInputNodes; nodeIn++)
            {
                weightedInput += inputs[nodeIn] * weights[nodeIn, nodeOut];
            }
            learnData.weightedInputs[nodeOut] = weightedInput;
        }

        // Apply activation function
        for (int i = 0; i < learnData.activations.Length; i++)
        {
            learnData.activations[i] = activation.Activate(learnData.weightedInputs, i);
        }

        return learnData.activations;
    }
    
    public void ApplyGradients(double learnRate, double regularization, double momentum)
    {
        double weightDecay = (1 - regularization * learnRate);

        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights.GetLength(1); j++)
            {
                double weight = weights[i,j];
                double velocity = weightVelocities[i,j] * momentum - costGradientW[i,j] * learnRate;
                weightVelocities[i, j] = velocity;
                weights[i, j] = weight * weightDecay + velocity;
                costGradientW[i, j] = 0;
            }
        }


        for (int i = 0; i < biases.Length; i++)
        {
            double velocity = biasVelocities[i] * momentum - costGradientB[i] * learnRate;
            biasVelocities[i] = velocity;
            biases[i] += velocity;
            costGradientB[i] = 0;
        }
    }
    
    // Calculate the "node values" for the output layer. This is an array containing for each node:
    // the partial derivative of the cost with respect to the weighted input
    public void CalculateOutputLayerNodeValues(LayerLearnData layerLearnData, double[] expectedOutputs, ICost cost)
    {
        for (int i = 0; i < layerLearnData.nodeValues.Length; i++)
        {
            // Evaluate partial derivatives for current node: cost/activation & activation/weightedInput
            double costDerivative = cost.CostDerivative(layerLearnData.activations[i], expectedOutputs[i]);
            double activationDerivative = activation.Derivative(layerLearnData.weightedInputs, i);
            layerLearnData.nodeValues[i] = costDerivative * activationDerivative;
        }
    }
    
    public void CalculateHiddenLayerNodeValues(LayerLearnData layerLearnData, Layer oldLayer, double[] oldNodeValues)
    {
        for (int newNodeIndex = 0; newNodeIndex < numOutputNodes; newNodeIndex++)
        {
            double newNodeValue = 0;
            for (int oldNodeIndex = 0; oldNodeIndex < oldNodeValues.Length; oldNodeIndex++)
            {
                // Partial derivative of the weighted input with respect to the input
                double weightedInputDerivative = oldLayer.weights[newNodeIndex, oldNodeIndex];
                newNodeValue += weightedInputDerivative * oldNodeValues[oldNodeIndex];
            }
            newNodeValue *= activation.Derivative(layerLearnData.weightedInputs, newNodeIndex);
            layerLearnData.nodeValues[newNodeIndex] = newNodeValue;
        }

    }
    
    public void UpdateGradients(LayerLearnData layerLearnData)
    {
        // Update cost gradient with respect to weights (lock for multithreading)
        lock (costGradientW)
        {
            for (int nodeOut = 0; nodeOut < numOutputNodes; nodeOut++)
            {
                double nodeValue = layerLearnData.nodeValues[nodeOut];
                for (int nodeIn = 0; nodeIn < numInputNodes; nodeIn++)
                {
                    // Evaluate the partial derivative: cost / weight of current connection
                    double derivativeCostWrtWeight = layerLearnData.inputs[nodeIn] * nodeValue;
                    // The costGradientW array stores these partial derivatives for each weight.
                    // Note: the derivative is being added to the array here because ultimately we want
                    // to calculate the average gradient across all the data in the training batch
                    costGradientW[nodeIn, nodeOut] += derivativeCostWrtWeight;
                }
            }
        }

        // Update cost gradient with respect to biases (lock for multithreading)
        lock (costGradientB)
        {
            for (int nodeOut = 0; nodeOut < numOutputNodes; nodeOut++)
            {
                // Evaluate partial derivative: cost / bias
                double derivativeCostWrtBias = 1 * layerLearnData.nodeValues[nodeOut];
                costGradientB[nodeOut] += derivativeCostWrtBias;
            }
        }
    }
    
    public void InitializeRandomWeights(System.Random rng)
    {
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights.GetLength(1); j++)
            {
                weights[i,j] = RandomInNormalDistribution(rng, 0, 1) / Sqrt(numInputNodes);
            }
        }

        double RandomInNormalDistribution(System.Random rng, double mean, double standardDeviation)
        {
            double x1 = 1 - rng.NextDouble();
            double x2 = 1 - rng.NextDouble();

            double y1 = Sqrt(-2.0 * Log(x1)) * Cos(2.0 * PI * x2);
            return y1 * standardDeviation + mean;
        }
    }
}