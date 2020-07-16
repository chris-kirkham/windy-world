using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    /// <summary>
    /// Interface for a hash key class to be used in the wind field. 
    /// An interface, rather than an abstract class, so it can be extended by a struct and therefore made blittable
    /// </summary>
    /// <typeparam name="KeyType"></typeparam>
    public interface IHashKey<KeyType>
    {
        KeyType GetKey();

        int GetHashCode();

        bool Equals(object other);

        bool Equals(IHashKey<KeyType> other);
    }
}

