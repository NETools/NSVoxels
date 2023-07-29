using System;
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

            using (var fs = new FileStream(path, FileMode.Open))
            {
#pragma warning disable SYSLIB0011 // Typ oder Element ist veraltet
				RAWXNSFile = (int[])binaryFormatter.Deserialize(fs);
#pragma warning restore SYSLIB0011 // Typ oder Element ist veraltet
			}

        }

    }
}
