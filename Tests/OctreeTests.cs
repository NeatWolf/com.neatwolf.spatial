using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class OctreeTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void OctreeSimplePasses()
    {
        // Use the Assert class to test conditions
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator OctreeWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
    
        private Octree<AudioVolumePortal> testTree;
    
        [SetUp]
        public void Setup()
        {
            // Initialize the octree before each test
            testTree = new Octree<AudioVolumePortal>(new Vector3(0, 0, 0), new Vector3(5, 5, 5), 3, 1, 3);
        }
    
        [Test]
        public void Insert_Node_Successfully()
        {
            var portal = new AudioVolumePortal();
            var node = new OctreeNode<AudioVolumePortal>(portal, new Vector3(1, 1, 1));
    
            bool result = testTree.Insert(node);
    
            Assert.IsTrue(result);
        }
    
        [Test]
        public void Remove_Node_Successfully()
        {
            var portal = new AudioVolumePortal();
            var node = new OctreeNode<AudioVolumePortal>(portal, new Vector3(1, 1, 1));
    
            testTree.Insert(node);
            bool result = testTree.Remove(node.Position);
    
            Assert.IsTrue(result);
        }
    
        [Test]
        public void Query_ReturnsCorrectNode()
        {
            var portal = new AudioVolumePortal();
            var node = new OctreeNode<AudioVolumePortal>(portal, new Vector3(1, 1, 1));
    
            testTree.Insert(node);
            var resultNode = testTree.Query(new Vector3(1, 1, 1));
    
            Assert.AreEqual(node, resultNode);
        }
    
        [Test]
        public void SphereCast_ReturnsNodesWithinRadius()
        {
            var portal1 = new AudioVolumePortal();
            var node1 = new OctreeNode<AudioVolumePortal>(portal1, new Vector3(1, 1, 1));
            var portal2 = new AudioVolumePortal();
            var node2 = new OctreeNode<AudioVolumePortal>(portal2, new Vector3(2, 2, 2));
            var portal3 = new AudioVolumePortal();
            var node3 = new OctreeNode<AudioVolumePortal>(portal3, new Vector3(4, 4, 4));
    
            testTree.Insert(node1);
            testTree.Insert(node2);
            testTree.Insert(node3);
    
            List<OctreeNode<AudioVolumePortal>> results = testTree.SphereCast(new Vector3(1, 1, 1), 2);
    
            Assert.Contains(node1, results);
            Assert.Contains(node2, results);
            Assert.IsFalse(results.Contains(node3));
        }
}

public class AudioVolumePortal
{
    // Sample class for test purposes. You can expand upon this or use your actual AudioVolumePortal class.
}