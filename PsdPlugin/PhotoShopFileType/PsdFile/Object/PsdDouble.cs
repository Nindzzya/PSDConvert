using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Endogine.Serialization;

namespace PhotoshopFile
{
   public class PsdDouble:PsdObject
    {
        /** The value. */
        private  double value;
        public long offset;

        /**
         * Instantiates a new psd double.
         *
         * @param stream the stream
         * @throws IOException Signals that an I/O exception has occurred.
         */
        public PsdDouble(BinaryReverseReader stream) {
            offset = stream.BaseStream.Position;
            value = stream.ReadDouble();
        }

        /**
         * Gets the value.
         *
         * @return the value
         */
        public double getValue() {
                return value;
        }

        
        public String toString() {
                return "doub:" + value;
        }

    }
}
