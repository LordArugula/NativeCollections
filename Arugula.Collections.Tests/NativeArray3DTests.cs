using NUnit.Framework;

namespace Arugula.Collections.Tests
{
    internal class NativeArray3DTests
    {
        const int length = 1 << 4;
        const int width = 1 << 4;
        const int height = 1 << 4;

        [Test]
        public void Length()
        {
            var array = new NativeArray3D<int>(length, width, height, Unity.Collections.Allocator.Temp);

            Assert.AreEqual(length * width * height, array.Length);
            array.Dispose();
        }

        [Test]
        public void Dispose()
        {
            var array = new NativeArray3D<int>(length, width, height, Unity.Collections.Allocator.Temp);

            Assert.IsTrue(array.IsCreated);
            array.Dispose();

            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                array[0, 0, 0] = 10;
            });

            Assert.False(array.IsCreated);

            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                array.Dispose();
            });
            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                array.Dispose(default);
            });

            array = new NativeArray3D<int>(length, width, height, Unity.Collections.Allocator.TempJob);

            Assert.IsTrue(array.IsCreated);
            array.Dispose(default);

            Assert.False(array.IsCreated);

            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                array.Dispose();
            });
            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                array.Dispose(default);
            });
        }

        [Test]
        public void Range()
        {
            var array = new NativeArray3D<int>(length, width, height, Unity.Collections.Allocator.Temp);

            Assert.DoesNotThrow(() =>
            {
                array[0, 0, 0] = 1;
                array[0, width - 1, 0] = 1;
                array[0, width - 1, height - 1] = 1;
                array[0, 0, height - 1] = 1;
                array[length - 1, 0, height - 1] = 1;
                array[length - 1, 0, 0] = 10;
                array[length - 1, width - 1, 0] = 10;
                array[length - 1, width - 1, height - 1] = 10;
            });

            Assert.Throws<System.IndexOutOfRangeException>(() =>
            {
                array[-1, 0, 0] = 1;
            });

            Assert.Throws<System.IndexOutOfRangeException>(() =>
            {
                array[0, -1, 0] = 1;
            });

            Assert.Throws<System.IndexOutOfRangeException>(() =>
            {
                array[0, 0, -1] = 1;
            });

            Assert.Throws<System.IndexOutOfRangeException>(() =>
            {
                array[length, 0, 0] = 1;
            });

            Assert.Throws<System.IndexOutOfRangeException>(() =>
            {
                array[0, width, 0] = 1;
            });

            Assert.Throws<System.IndexOutOfRangeException>(() =>
            {
                array[0, 0, height] = 1;
            });

            array.Dispose();
        }

        [Test]
        public void IndexOrder()
        {
            var array = new NativeArray3D<int>(length, width, height, Unity.Collections.Allocator.Temp);
            int n = 0;
            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        Assert.AreEqual(n, (x * length + y) * width + z);
                        array[x, y, z] = n++;
                    }
                }
            }

            n = 0;
            foreach (var item in array)
            {
                Assert.AreEqual(n++, item);
            }

            array.Dispose();
        }

        [Test]
        public void CopyFrom()
        {
            var src = new int[length, width, height];
            for (int x = 0, n = 0; x < length; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    for (int z = 0; z < height; z++, n++)
                    {
                        src[x, y, z] = n;
                    }
                }
            }

            var dstA = new NativeArray3D<int>(length, width, height, Unity.Collections.Allocator.Temp);
            dstA.CopyFrom(src);
            Assert.AreEqual(src.GetLength(0), dstA.Length0);
            Assert.AreEqual(src.GetLength(1), dstA.Length1);
            Assert.AreEqual(src.GetLength(2), dstA.Length2);
            Assert.AreEqual(src.Length, dstA.Length);

            var dstB = new NativeArray3D<int>(src, Unity.Collections.Allocator.Temp);
            Assert.AreEqual(src.GetLength(0), dstB.Length0);
            Assert.AreEqual(src.GetLength(1), dstB.Length1);
            Assert.AreEqual(src.GetLength(2), dstB.Length2);
            Assert.AreEqual(src.Length, dstB.Length);

            var dstC = new NativeArray3D<int>(dstA, Unity.Collections.Allocator.Temp);
            Assert.AreEqual(src.GetLength(0), dstC.Length0);
            Assert.AreEqual(src.GetLength(1), dstC.Length1);
            Assert.AreEqual(src.GetLength(2), dstC.Length2);
            Assert.AreEqual(src.Length, dstC.Length);

            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        Assert.AreEqual(src[x, y, z], dstA[x, y, z]);
                        Assert.AreEqual(src[x, y, z], dstB[x, y, z]);
                        Assert.AreEqual(src[x, y, z], dstC[x, y, z]);
                    }
                }
            }

            dstA.Dispose();
            dstB.Dispose();
            dstC.Dispose();
        }

        [Test]
        public void CopyTo()
        {
            var src = new NativeArray3D<int>(length, width, height, Unity.Collections.Allocator.Temp);
            for (int x = 0, n = 0; x < length; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    for (int z = 0; z < height; z++, n++)
                    {
                        src[x, y, z] = n;
                    }
                }
            }

            var dstA = new int[length, width, height];
            src.CopyTo(dstA);
            Assert.AreEqual(src.Length0, dstA.GetLength(0));
            Assert.AreEqual(src.Length1, dstA.GetLength(1));
            Assert.AreEqual(src.Length2, dstA.GetLength(2));
            Assert.AreEqual(src.Length, dstA.Length);

            var dstB = new NativeArray3D<int>(dstA, Unity.Collections.Allocator.Temp);
            Assert.AreEqual(src.Length0, dstB.Length0);
            Assert.AreEqual(src.Length1, dstB.Length1);
            Assert.AreEqual(src.Length2, dstB.Length2);
            Assert.AreEqual(src.Length, dstB.Length);

            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        Assert.AreEqual(src[x, y, z], dstA[x, y, z]);
                        Assert.AreEqual(src[x, y, z], dstB[x, y, z]);
                    }
                }
            }

            src.Dispose();
            dstB.Dispose();
        }

        [Test]
        public void Enumerator()
        {
            var nativeArray = new NativeArray3D<int>(length, width, height, Unity.Collections.Allocator.Temp);
            var managedArray = new int[length, width, height];
            int n = 0;
            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    for (int z = 0; z < height; z++, n++)
                    {
                        nativeArray[x, y, z] = n;
                        managedArray[x, y, z] = n;
                    }
                }
            }

            n = 0;
            foreach (var item in nativeArray)
            {
                Assert.AreEqual(n++, item);
            }

            n = 0;
            foreach (var item in managedArray)
            {
                Assert.AreEqual(n++, item);
            }

            nativeArray.Dispose();
        }

        [Test]
        public void Flatten()
        {
            var src = new NativeArray3D<int>(length, width, height, Unity.Collections.Allocator.Temp);

            for (int x = 0, n = 0; x < length; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    for (int z = 0; z < height; z++, n++)
                    {
                        src[x, y, z] = n;
                    }
                }
            }

            int[] managedArr = src.Flatten();
            Unity.Collections.NativeArray<int> nativeArr = src.Flatten(Unity.Collections.Allocator.Temp);

            Assert.AreEqual(src.Length, managedArr.Length);
            Assert.AreEqual(nativeArr.Length, managedArr.Length);

            for (int x = 0, n = 0; x < length; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    for (int z = 0; z < height; z++, n++)
                    {
                        Assert.AreEqual(n, managedArr[n]);
                        Assert.AreEqual(n, nativeArr[n]);
                    }
                }
            }

            src.Dispose();
            nativeArr.Dispose();
        }

        [Test]
        public void ToArray()
        {
            var src = new NativeArray3D<int>(length, width, height, Unity.Collections.Allocator.Temp);

            for (int x = 0, n = 0; x < length; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    for (int z = 0; z < height; z++, n++)
                    {
                        src[x, y, z] = n;
                    }
                }
            }

            var output = src.ToArray();
            Assert.AreEqual(src.Length0, output.GetLength(0));
            Assert.AreEqual(src.Length1, output.GetLength(1));
            Assert.AreEqual(src.Length2, output.GetLength(2));
            Assert.AreEqual(src.Length, output.Length);

            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        Assert.AreEqual(src[x, y, z], output[x, y, z]);
                    }
                }
            }

            src.Dispose();
        }

        public struct ManagedStructTest
        {
            public UnityEngine.GameObject gameObject;
        }

        [Test]
        public void ThrowsIfTypeIsNotUnmanaged()
        {
            Assert.Throws<System.NotSupportedException>(() =>
            {
                NativeArray2D<ManagedStructTest> array = new NativeArray2D<ManagedStructTest>(10, 10, Unity.Collections.Allocator.Temp);

                array.Dispose();
            });
        }
    }
}
