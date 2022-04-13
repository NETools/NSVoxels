﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Concrete.RAWLoader
{
    public class RAWLoaderXNS
    {

        public int[] RAWXNSFile;

        public RAWLoaderXNS(string path)
        {

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            
            RAWXNSFile = (int[]) binaryFormatter.Deserialize(new FileStream(path, FileMode.Open));


        }

    }
}
