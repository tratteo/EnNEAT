﻿using UnityEngine;

public class Manager : MonoBehaviour
{
    [Header("Manager")]
    public Transform startPoint;
    public GameObject carIndividualPrefab;
    public float timeScale = 1F;

    /// <summary>
    ///   Instantiate and initialize the Neural Net of an individual, if null is passed as dna, it
    ///   will be created a Net with a random set of weights
    ///   <para> Return: the instantiated CarIndividual </para>
    /// </summary>
    protected virtual CarIndividual InstantiateAndInitializeIndividual(DNA dna, string name)
    {
        CarIndividual car = Instantiate(carIndividualPrefab, startPoint.localPosition, startPoint.localRotation).GetComponent<CarIndividual>();
        car.gameObject.name = name == null ? "Car" : name;
        DNA individualDna;
        if (dna == null)
        {
            individualDna = new DNA(car.topology);
        }
        else
        {
            individualDna = new DNA(dna.topology, dna.weights);
        }
        car.InitializeNeuralNet(individualDna);
        return car;
    }
}