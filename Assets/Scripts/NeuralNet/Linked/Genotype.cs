﻿using Assets.Scripts.Stores;
using Assets.Scripts.TUtils.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.NeuralNet
{
    [Serializable]
    public class Genotype
    {
        public List<NodeGene> all;
        public List<NodeGene> hidden;
        public List<NodeGene> inputs;
        public List<LinkGene> links;
        public List<NodeGene> outputs;

        #region Properties

        public int HiddenCount
        {
            get => hidden.Count;
        }

        public int InputCount
        {
            get => inputs.Count;
        }

        public int LinkCount
        {
            get => links.Count;
        }

        public int NodeCount
        {
            get; private set;
        }

        public int OutputCount
        {
            get => outputs.Count;
        }

        #endregion Properties

        public Genotype(TopologyDescriptor descriptor) : this()
        {
            int lenght = descriptor.inputCount + descriptor.hiddenCount + descriptor.outputCount;
            for (int i = 0; i < lenght; i++)
            {
                NodeGene current = new NodeGene(-i - 1, TMath.Tanh);
                if (i < descriptor.inputCount)
                {
                    current.SetType(NodeType.INPUT);
                }
                else if (i > lenght - descriptor.outputCount - 1)
                {
                    current.SetType(NodeType.OUTPUT);
                }
                else
                {
                    current.SetType(NodeType.HIDDEN);
                }
                AddNode(current);
            }

            int linkN = 1;
            foreach (LinkDescriptor link in descriptor.links)
            {
                AddLink(new LinkDescriptor(-link.fromId, -link.toId), linkN++, UnityEngine.Random.Range(-1F, 1F));
            }
        }

        public Genotype()
        {
            inputs = new List<NodeGene>();
            outputs = new List<NodeGene>();
            hidden = new List<NodeGene>();
            all = new List<NodeGene>();
            links = new List<LinkGene>();
        }

        /// <summary>
        ///   Add a link and the nodes involved, if not already present. If the node is present, add the link to its incoming link list
        /// </summary>
        /// <param name="newLink"> </param>
        public void AddLinkAndNodes(LinkGene newLink)
        {
            if (links.Contains(newLink))
            {
                return;
            }
            NodeGene from = null;
            NodeGene to = null;
            foreach (NodeGene node in all)
            {
                if (node.Equals(newLink.From()))
                {
                    from = node;
                }
                if (node.Equals(newLink.To()))
                {
                    to = node;
                }
                if (from != null && to != null) break;
            }
            if (to == null)
            {
                to = newLink.To().CopyNoLinks();
            }
            if (from == null)
            {
                from = newLink.From().CopyNoLinks();
            }
            LinkGene currentNew = new LinkGene(from, to, newLink.GetWeight(), newLink.GetInnovationNumber());
            to.AddIncomingLink(currentNew);
            from.AddOutgoingLink(currentNew);
            AddNode(to);
            AddNode(from);
            links.Add(currentNew);
        }

        /// <summary>
        ///   Add a nodes only if not already present
        /// </summary>
        /// <param name="nodeCopy"> </param>
        /// <returns> </returns>
        public NodeGene AddNode(NodeGene nodeCopy)
        {
            if (all.Contains(nodeCopy)) return nodeCopy;

            if (nodeCopy.GetType().Equals(NodeType.INPUT))
            {
                inputs.Add(nodeCopy);
            }
            else if (nodeCopy.GetType().Equals(NodeType.OUTPUT))
            {
                outputs.Add(nodeCopy);
            }
            else if (nodeCopy.GetType().Equals(NodeType.HIDDEN))
            {
                hidden.Add(nodeCopy);
            }

            all.Add(nodeCopy);
            NodeCount++;
            return nodeCopy;
        }

        /// <summary>
        ///   Perform a crossover between the two genotypes and get the result
        /// </summary>
        /// <param name="partner"> </param>
        /// <param name="thisFitness"> </param>
        /// <param name="partnerFitness"> </param>
        /// <returns> </returns>
        public Genotype Crossover(Genotype partner, double thisFitness, double partnerFitness)
        {
            Genotype childGen = new Genotype();
            //TODO IMPROVE

            List<LinkGene> remaining = new List<LinkGene>(links);
            List<LinkGene> partnerRemaining = new List<LinkGene>(partner.links);
            // Zip togheter the links that have the same innovation number
            List<Tuple<LinkGene, LinkGene>> zippedLinks = TUtilsProvider.ZipWithPredicate(links, partner.links, (item1, item2) => item1.GetInnovationNumber().Equals(item2.GetInnovationNumber()));

            //Add to che child all the matching genes(links)
            foreach (Tuple<LinkGene, LinkGene> gene in zippedLinks)
            {
                LinkGene copy;
                if (/*thisFitness > partnerFitness*/UnityEngine.Random.Range(0F, 1F) < 0.5F)
                {
                    copy = gene.Item1;
                }
                else
                {
                    copy = gene.Item2;
                }

                childGen.AddLinkAndNodes(copy);
                remaining.RemoveAll(item => item.GetInnovationNumber().Equals(copy.GetInnovationNumber()));
                partnerRemaining.RemoveAll(item => item.GetInnovationNumber().Equals(copy.GetInnovationNumber()));
            }

            // At this point all common genes are added we add all the disjoint genes from the fittest
            if (thisFitness > partnerFitness)
            {
                foreach (LinkGene gene in remaining)
                {
                    childGen.AddLinkAndNodes(gene);
                }
            }
            else
            {
                foreach (LinkGene gene in partnerRemaining)
                {
                    childGen.AddLinkAndNodes(gene);
                }
            }

            return childGen;
        }

        /// <summary>
        ///   Get the genotype distance between this genotype and another one
        /// </summary>
        /// <param name="to"> </param>
        /// <returns> </returns>
        public float GetTopologicalDistance(Genotype to)
        {
            int maxGenomes = this.LinkCount > to.LinkCount ? this.LinkCount : to.LinkCount;
            int genesDifference = this.LinkCount - to.LinkCount;
            List<Tuple<LinkGene, LinkGene>> zippedLinks = TUtilsProvider.ZipWithPredicate(links, to.links, (item1, item2) => item1.GetInnovationNumber().Equals(item2.GetInnovationNumber()));
            float differenceSum = 0;
            foreach (Tuple<LinkGene, LinkGene> current in zippedLinks)
            {
                differenceSum += TMath.Abs((float)(current.Item1.GetWeight() - current.Item2.GetWeight()));
            }
            float averageDiff = differenceSum / zippedLinks.Count;
            float c = 1F, c2 = 0.3F;
            //Debug.Log("current diff: " + (c * genesDifference) / maxGenomes + (c2 * averageDiff));
            return (c * genesDifference) / maxGenomes + (c2 * averageDiff);
        }

        /// <summary>
        ///   Mutate this genotype based on the specified probabilities
        /// </summary>
        /// <param name="mutation"> </param>
        public void Mutate(BreedingParameters mutation)
        {
            if (UnityEngine.Random.Range(0F, 1F) < mutation.splitLinkProb)
            {
                // 1. Select a random link to be mutated
                // 2. Create a new node
                // 3. Create a topology mutation based on the random link selected, set its innovation number and set the same number to the
                // random link
                LinkGene random = links.ElementAt(UnityEngine.Random.Range(0, links.Count));
                NodeGene newNode = new NodeGene(UnityEngine.Random.Range(0, int.MaxValue), TMath.Tanh, NodeType.HIDDEN);
                TopologyMutation topologyMutation = new TopologyMutation(TopologyMutationType.SPLIT_LINK, random);
                topologyMutation.SetInnovationNumber(GlobalParams.GetGenerationInnovationNumber(topologyMutation));
                random.SetInnovationNumber(topologyMutation.GetInnovationNumber());

                // 1. Create a new link that will connect the new node
                // 2. Create a topology mutation based on the new link, set its innovation number and set the same number to the link
                // 3. Redirect the connections:
                // Before: A --random--> B
                // After: A --newLink--> C --random--> B
                LinkGene newLink = new LinkGene(random.From(), newNode, 1F);
                TopologyMutation topologyMutation1 = new TopologyMutation(TopologyMutationType.SPLIT_LINK, newLink);
                topologyMutation1.SetInnovationNumber(GlobalParams.GetGenerationInnovationNumber(topologyMutation1));
                newLink.SetInnovationNumber(topologyMutation1.GetInnovationNumber());
                random.SetFrom(newNode);

                // Add the new node and the link
                AddNode(newNode);
                AddLinkAndNodes(newLink);
                UnityEngine.Debug.Log("SPLITTED");
            }
            if (UnityEngine.Random.Range(0F, 1F) < mutation.addLinkProb)
            {
                // Create a list of all nodes except the output nodes and select the random From node
                List<NodeGene> temp = new List<NodeGene>(inputs);
                temp.AddRange(hidden);
                NodeGene fromRandom = temp.ElementAt(UnityEngine.Random.Range(0, temp.Count));

                // 1. Create a list of all nodes except the input nodes
                // 2. Remove the already selected node if present to prevent recurrent link
                // 3. Remove all the nodes in the list that are topologically behind the randomly selected node, only if they are not output nodes
                // 4. If at least one node remains:
                // - Select a random To node
                // - Create a new link connecting the two nodes
                // - If the link is not already present, create a topology mutation, retrieve the innovation number and add the link
                temp = new List<NodeGene>(outputs);
                temp.AddRange(hidden);
                temp.Remove(fromRandom);
                List<NodeGene> newList = new List<NodeGene>(temp);
                foreach (NodeGene gene in newList)
                {
                    if (fromRandom.id > gene.id && gene.GetType() != NodeType.OUTPUT)
                    {
                        temp.Remove(gene);
                    }
                }
                if (temp.Count > 0)
                {
                    NodeGene toRandom = temp.ElementAt(UnityEngine.Random.Range(0, temp.Count));
                    LinkGene newLink = new LinkGene(fromRandom, toRandom, 1F);
                    if (!links.Contains(newLink))
                    {
                        TopologyMutation topologyMutation = new TopologyMutation(TopologyMutationType.ADD_LINK, newLink);
                        topologyMutation.SetInnovationNumber(GlobalParams.GetGenerationInnovationNumber(topologyMutation));
                        newLink.SetInnovationNumber(topologyMutation.GetInnovationNumber());
                        AddLinkAndNodes(newLink);
                        UnityEngine.Debug.Log("ADDED");
                    }
                }
            }
            // For each link of this genotype try to mutate it
            foreach (LinkGene gene in links)
            {
                if (UnityEngine.Random.Range(0F, 1F) < mutation.weightChangeProb)
                {
                    int random = UnityEngine.Random.Range(0, LinkCount);
                    links[random].SetWeight(UnityEngine.Random.Range(-1F, 1F));
                }
            }
        }

        /// <summary>
        ///   Get the string representation of this genotype
        /// </summary>
        /// <returns> </returns>
        public override string ToString()
        {
            string output = "Inputs: " + InputCount + ", Outputs: " + OutputCount + ", Hidden: " + HiddenCount + ", Total: " + NodeCount + ", Link count: " + LinkCount + "\n";
            foreach (NodeGene node in all)
            {
                if (node.GetIncomingLinks().Count > 0)
                    output += "To node " + node.id + ":\n";
                foreach (LinkGene gene in node.GetIncomingLinks())
                {
                    output += gene.ToString() + "\n";
                }
            }
            return output;
        }

        /// <summary>
        ///   Add a link described by a link descriptor only if the two nodes involved are present
        /// </summary>
        /// <param name="link"> </param>
        /// <param name="innovationNumber"> </param>
        /// <param name="weight"> </param>
        /// <returns> </returns>
        private LinkGene AddLink(LinkDescriptor link, int innovationNumber, double weight)
        {
            LinkGene newLink = null;
            NodeGene from = null;
            NodeGene to = null;
            foreach (NodeGene node in all)
            {
                if (node.id.Equals(link.fromId))
                {
                    from = node;
                }
                if (node.id.Equals(link.toId))
                {
                    to = node;
                }
                if (from != null && to != null) break;
            }
            if (to != null && from != null)
            {
                newLink = new LinkGene(from, to, weight, innovationNumber);
                to.AddIncomingLink(newLink);
                from.AddOutgoingLink(newLink);
                links.Add(newLink);
            }
            return newLink;
        }
    }
}