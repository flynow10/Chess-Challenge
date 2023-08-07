using System;
using System.Linq;

namespace ChessChallenge.MyBot.Neural_Network;

public class NeuralNetwork
{
    public readonly Layer[] layers;
    public readonly int[] layerSizes;
    
    private Random rng;
    NetworkLearnData[] batchLearnData;
    // private ICost cost;

    public NeuralNetwork(params int[] layerSizes)
    {
        this.layerSizes = layerSizes;
        rng = new Random();
        
        layers = new Layer[layerSizes.Length - 1];
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i] = new Layer(layerSizes[i], layerSizes[i+1], rng);
        }
    }

    public double[] CalculateOutputs(double[] inputs)
    {
        foreach (Layer layer in layers)
        {
            inputs = layer.CalculateOutputs(inputs);
        }

        return inputs;
    }

    public void Learn(DataPoint[] trainingData, double learnRate, double regularization = 0, double momentum = 0)
    {

        if (batchLearnData == null || batchLearnData.Length != trainingData.Length)
        {
            batchLearnData = new NetworkLearnData[trainingData.Length];
            for (int i = 0; i < batchLearnData.Length; i++)
            {
                batchLearnData[i] = new NetworkLearnData(layers);
            }
        }

        System.Threading.Tasks.Parallel.For(0, trainingData.Length, (i) =>
        {
            UpdateGradients(trainingData[i], batchLearnData[i]);
        });


        // Update weights and biases based on the calculated gradients
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i].ApplyGradients(learnRate / trainingData.Length, regularization, momentum);
        }
    }
    
    void UpdateGradients(DataPoint data, NetworkLearnData learnData)
    {
        // Feed data through the network to calculate outputs.
        // Save all inputs/weightedinputs/activations along the way to use for backpropagation.
        double[] inputsToNextLayer = data.boardRepresentation.Select(Convert.ToDouble).ToArray();

        for (int i = 0; i < layers.Length; i++)
        {
            inputsToNextLayer = layers[i].CalculateOutputs(inputsToNextLayer, learnData.layerData[i]);
        }

        // -- Backpropagation --
        int outputLayerIndex = layers.Length - 1;
        Layer outputLayer = layers[outputLayerIndex];
        LayerLearnData outputLearnData = learnData.layerData[outputLayerIndex];

        // Update output layer gradients
        // outputLayer.CalculateOutputLayerNodeValues(outputLearnData, data.expectedOutputs, cost);
        outputLayer.UpdateGradients(outputLearnData);

        // Update all hidden layer gradients
        for (int i = outputLayerIndex - 1; i >= 0; i--)
        {
            LayerLearnData layerLearnData = learnData.layerData[i];
            Layer hiddenLayer = layers[i];

            hiddenLayer.CalculateHiddenLayerNodeValues(layerLearnData, layers[i + 1], learnData.layerData[i + 1].nodeValues);
            hiddenLayer.UpdateGradients(layerLearnData);
        }

    }
}

public class NetworkLearnData
{
    public LayerLearnData[] layerData;

    public NetworkLearnData(Layer[] layers)
    {
        layerData = new LayerLearnData[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            layerData[i] = new LayerLearnData(layers[i]);
        }
    }
}

public class LayerLearnData
{
    public double[] inputs;
    public double[] weightedInputs;
    public double[] activations;
    public double[] nodeValues;

    public LayerLearnData(Layer layer)
    {
        weightedInputs = new double[layer.numOutputNodes];
        activations = new double[layer.numOutputNodes];
        nodeValues = new double[layer.numOutputNodes];
    }
}