using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECC_final.ECC
{
    public class EllipticCurveAlgorithm
    {
        public static (int d, (int x, int y) Q, (int x, int y) C1, List<(int x, int y, int offset)> C2, EllipticCurve curve)
Encode(string originalText)
        {
            int p = 257;
            byte[] originalBytes = Encoding.UTF8.GetBytes(originalText);

            while (true)
            {
                EllipticCurve curve = EllipticCurve.GenerateValidCurve(p);

                List<(int x, int y, int offset)> originalPoint = EllipticCurve.MapToPointList(originalBytes, curve);
                Console.WriteLine(originalText);

                while (originalPoint.Count != originalBytes.Length)
                {
                    curve = EllipticCurve.GenerateValidCurve(p);
                    originalPoint = EllipticCurve.MapToPointList(originalBytes, curve);
                }

                // Generate keys
                var G = curve.GenerateRandomPoint();
                var key = EllipticCurve.GenerateKeys(curve.p, G);
                var Q = key.Q; // Public key
                int d = key.d; // Private key

                // Encoding process
                int k = 15;
                List<(int x, int y, int offset)> result = new List<(int x, int y, int offset)>();
                var C1 = EllipticCurve.Multiply(G, k, curve.p);

                foreach (var x in originalPoint)
                {
                    var C2 = EllipticCurve.Add((x.x, x.y), EllipticCurve.Multiply(Q, k, curve.p), curve.p);
                    result.Add((C2.x, C2.y, x.offset));
                }
                string decodeText = Decode(d, C1, result, curve);


                if (decodeText == originalText)
                {
                    return (d, Q, C1, result, curve); 
                }
            }
        }


        public static string Decode(int d, (int x, int y) C1, List<(int x, int y, int offset)> C2, EllipticCurve curve)
        {
            List<byte> decodeBytes = new List<byte>();
            foreach (var x in C2)
            {
                var dC1 = EllipticCurve.Multiply(C1, d, curve.p);
                var mPoint = EllipticCurve.Add((x.x, x.y), (dC1.x, (-dC1.y + curve.p) % curve.p), curve.p);

                int originalX = (mPoint.x - x.offset + curve.p) % curve.p;
                decodeBytes.Add((byte)originalX);
            }
            return Encoding.UTF8.GetString(decodeBytes.ToArray());
        }
    }
}
