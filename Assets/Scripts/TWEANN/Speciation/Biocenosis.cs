﻿using System.Collections.Generic;

namespace Assets.Scripts.TWEANN
{
    public class Biocenosis
    {
        private List<Species> speciesList;
        private float speciesSharingThreshold;

        public Biocenosis(float speciesSharingThreshold)
        {
            speciesList = new List<Species>();
            this.speciesSharingThreshold = speciesSharingThreshold;
        }

        public Biocenosis(List<Species> speciesList, float speciesSharingThreshold) : this(speciesSharingThreshold)
        {
            this.speciesList = speciesList;
        }

        /// <summary>
        ///   <b> Speciates the population </b><br> </br>
        ///   1. Split oranism into species based on the Biocenosis sharing threshold <br> </br>
        ///   2. Calculate the next generation expected number per species and total number of individuals
        /// </summary>
        /// <param name="population"> </param>
        public void Speciate(IIndividual[] population)
        {
            Reset();
            int lenght = population.Length;
            for (int i = 0; i < lenght; i++)
            {
                AddToSpeciesOrCreate(population[i]);
            }
            CalculateSpeciesExpectedNumber();

            //foreach (IIndividual individual in population)
            //{
            //    UnityEngine.Debug.Log(((MonoBehaviour)individual).name + ", fitness adj: " + individual.ProvideFitness());
            //}
        }

        private void AddToSpeciesOrCreate(IIndividual individual)
        {
            foreach (Species species in speciesList)
            {
                if (species.Belongs(individual, speciesSharingThreshold))
                {
                    species.AddToSpecies(individual);
                    return;
                }
            }
            Species newSpecies = new Species();
            newSpecies.AddToSpecies(individual);
            AddSpecies(newSpecies);
        }

        private void AddSpecies(Species species)
        {
            if (!speciesList.Contains(species))
            {
                speciesList.Add(species);
            }
        }

        private void CalculateSpeciesExpectedNumber()
        {
            foreach (Species species in speciesList)
            {
                foreach (IIndividual individual in species.GetIndividuals())
                {
                    individual.SetFitness(individual.ProvideFitness() / species.GetIndividualCount());
                }
            }
            double sum = 0;
            foreach (Species species in speciesList)
            {
                sum += species.GetFitnessSum();
            }

            double averageFitness = sum / GetTotalIndividualNumber();

            foreach (Species species in speciesList)
            {
                species.SetNewExpectedIndividualCount((int)(species.GetFitnessSum() / averageFitness));
            }
            Purge();
        }

        public int GetExpectedIndividualNumber()
        {
            int count = 0;
            foreach (Species species in speciesList)
            {
                count += species.GetExpectedIndividualCount();
            }
            return count;
        }

        public int GetTotalIndividualNumber()
        {
            int count = 0;
            foreach (Species species in speciesList)
            {
                count += species.GetIndividualCount();
            }
            return count;
        }

        public float GetSharingTreshold()
        {
            return speciesSharingThreshold;
        }

        public List<Species> GetSpeciesList()
        {
            return speciesList;
        }

        public IIndividual GetCurrentFittest()
        {
            double max = 0;
            IIndividual fittest = null;
            foreach (Species species in speciesList)
            {
                IIndividual current = species.GetChamp();
                if (current != null && current.ProvideFitness() > max)
                {
                    fittest = species.GetChamp();
                    max = fittest.ProvideFitness();
                }
            }
            return fittest;
        }

        private void Reset()
        {
            foreach (Species species in speciesList)
            {
                species.Reset();
            }
        }

        private void Purge()
        {
            speciesList.RemoveAll((current) => current.GetExpectedIndividualCount() == 0);
        }

        public override string ToString()
        {
            return "Sharing threshold: " + speciesSharingThreshold + ", Number of species: " + speciesList.Count + ", Tot: " + GetExpectedIndividualNumber();
        }
    }
}