using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECC_final.ECC
{
    public class EllipticCurve
    {
        public readonly int a;
        public readonly int b;
        public readonly int p; // prime

        public EllipticCurve(int a, int b, int p)
        {
            this.a = a;
            this.b = b;
            this.p = p;

            // Kiểm tra điều kiện hợp lệ của đường cong elliptic: 4a^3 + 27b^2 ≠ 0 (mod p)
            if ((4 * ((a * a % p) * a % p) % p + 27 * (b * b % p) % p) % p == 0)
            {
                throw new Exception("Invalid Elliptic Curve");
            }
        }
        // Hàm sinh a, b hợp lệ
        public static EllipticCurve GenerateValidCurve(int p)
        {
            Random rand = new Random();
            int a, b;

            while (true)
            {
                // [0, p-1]
                a = rand.Next(p);
                b = rand.Next(p);

                try
                {
                    return new EllipticCurve(a, b, p);
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }

        public static List<(int x, int y, int offset)> MapToPointList(byte[] bytes, EllipticCurve curve)
        {
            List<(int x, int y, int offset)> result = new List<(int x, int y, int offset)>();
            foreach (byte b in bytes)
            {
                int x = b & 0xFF;
                bool found = false;

                for (int offset = 0; offset < curve.p; offset++)
                {
                    int currentX = (x + offset) % curve.p;
                    int rhs = (currentX * currentX * currentX + curve.a * currentX + curve.b) % curve.p;

                    for (int y = 0; y < curve.p; y++)
                    {
                        if ((y * y) % curve.p == rhs)
                        {
                            result.Add((currentX, y, offset)); // Lưu offset
                            found = true;
                            break;
                        }
                    }

                    if (found) break;
                }

                if (!found)
                {
                    throw new Exception($"Không thể ánh xạ byte {b} vào điểm trên đường cong.");
                }
            }

            return result;
        }



        public static (int d, (int x, int y) Q) GenerateKeys(int p, (int x, int y) G)
        {
            Random rand = new Random();
            int d = rand.Next(1, p); // Khóa riêng d: ngẫu nhiên từ 1 đến p-1

            // Tính khóa công khai Q = d * G
            var Q = Multiply(G, d, p);
            return (d, Q);
        }

        // Hàm nhân điểm trên đường cong elliptic: Q = k * P
        public static (int x, int y) Multiply((int x, int y) P, int k, int p)
        {
            (int x, int y) result = (0, 0);
            (int x, int y) temp = P;

            while (k > 0)
            {
                if ((k & 1) == 1)
                {
                    result = Add(result, temp, p);
                }


                temp = Add(temp, temp, p);
                k >>= 1;
            }

            return result;
        }

        // Hàm cộng hai điểm trên đường cong elliptic
        public static (int x, int y) Add((int x, int y) P1, (int x, int y) P2, int p)
        {
            if (P1 == (0, 0)) return P2;
            if (P2 == (0, 0)) return P1;

            int lambda;
            if (P1 == P2)
            {
                lambda = (3 * P1.x * P1.x % p) * ModInverse(2 * P1.y, p) % p;
            }
            else
            {
                lambda = (P2.y - P1.y) * ModInverse(P2.x - P1.x, p) % p;
            }

            int x3 = (lambda * lambda - P1.x - P2.x) % p;
            int y3 = (lambda * (P1.x - x3) - P1.y) % p;

            return ((x3 + p) % p, (y3 + p) % p);
        }

        // Tự động sinh điểm G
        public (int x, int y) GenerateRandomPoint()
        {
            Random rand = new Random();
            while (true)
            {
                int x = rand.Next(0, p);
                int rhs = (x * x % p * x % p + a * x % p + b % p) % p; // Tính x^3 + ax + b (mod p)

                // Kiểm tra xem rhs có phải là quadratic residue (tồn tại y sao cho y^2 ≡ rhs (mod p))
                int y = ModularSqrt(rhs, p);
                if (y != -1)
                {
                    return (x, y);
                }
            }
        }

        // Hàm tính modulo nghịch đảo
        public static int ModInverse(int a, int p)
        {
            return ModPow(a, p - 2, p);
        }

        // Hàm tính lũy thừa modulo
        public static int ModPow(int baseValue, int exp, int mod)
        {
            int result = 1;
            baseValue %= mod;
            while (exp > 0)
            {
                if ((exp & 1) == 1) result = (result * baseValue) % mod;
                baseValue = (baseValue * baseValue) % mod;
                exp >>= 1;
            }
            return result;
        }
        // Tìm căn bậc hai modulo p (sử dụng thuật toán Tonelli-Shanks)
        public static int ModularSqrt(int a, int p)
        {
            if (LegendreSymbol(a, p) != 1) return -1; // Không có căn bậc hai
            if (a == 0) return 0;
            if (p == 2) return a;

            // Tonelli-Shanks
            int q = p - 1;
            int s = 0;
            while (q % 2 == 0)
            {
                q /= 2;
                s++;
            }

            if (s == 1)
            {
                return ModExp(a, (p + 1) / 4, p);
            }

            // Tìm z
            int z = 2;
            while (LegendreSymbol(z, p) != -1)
            {
                z++;
            }

            int m = s;
            int c = ModExp(z, q, p);
            int t = ModExp(a, q, p);
            int r = ModExp(a, (q + 1) / 2, p);

            while (t != 0 && t != 1)
            {
                int t2i = t;
                int i = 0;
                for (i = 1; i < m; i++)
                {
                    t2i = (t2i * t2i) % p;
                    if (t2i == 1) break;
                }

                int b = ModExp(c, 1 << (m - i - 1), p);
                m = i;
                c = (b * b) % p;
                t = (t * c) % p;
                r = (r * b) % p;
            }
            return r;
        }

        public static int LegendreSymbol(int a, int p)
        {
            int ls = ModExp(a, (p - 1) / 2, p);
            return (ls == p - 1) ? -1 : ls;
        }

        public static int ModExp(int baseValue, int exp, int mod)
        {
            int result = 1;
            baseValue = baseValue % mod;
            while (exp > 0)
            {
                if ((exp & 1) == 1)
                {
                    result = (result * baseValue) % mod;
                }
                exp = exp >> 1;
                baseValue = (baseValue * baseValue) % mod;
            }
            return result;
        }
    }
}
