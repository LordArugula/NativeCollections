using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Arugula.Collections.Tests
{
    public class NativeArray2DTests
    {
        [Test]
        public void LengthEquals_Length0xLength1()
        {
            var array = new NativeArray2D<int>(5, 3, Unity.Collections.Allocator.Temp);

            Assert.AreEqual(5 * 3, array.Length);
            array.Dispose();
        }

        [Test]
        public void SetInBounds_DoesNotThrow()
        {
            var array = new NativeArray2D<int>(5, 3, Unity.Collections.Allocator.Temp);
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Assert.DoesNotThrow(() => { array[x, y] = x * y; });
                    Assert.AreEqual(array[x, y], x * y);
                }
            }

            array.Dispose();
        }

        [Test]
        public void SetOutOfBounds_Throws()
        {
            var array = new NativeArray2D<int>(5, 3, Unity.Collections.Allocator.Temp);

            Assert.Throws<IndexOutOfRangeException>(() => { array[5, -1] = 10; });

            array.Dispose();
        }

        [Test]
        public void GetOutOfBounds_Throws()
        {
            var array = new NativeArray2D<int>(5, 3, Unity.Collections.Allocator.Temp);

            Assert.Throws<IndexOutOfRangeException>(() => { int outOfBounds = array[5, -1]; });
            array.Dispose();
        }

        [Test]
        public void EnumerateDoesNotThrow()
        {
            var array = new NativeArray2D<int>(5, 3, Unity.Collections.Allocator.Temp);

            Assert.DoesNotThrow(() =>
            {
                for (int x = 0; x < 5; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        array[x, y] = x * 3 + y;
                    }
                }

                int i = 0;
                foreach (var item in array)
                {
                    Assert.AreEqual(i++, item);
                }
                Assert.AreEqual(array.Length, i);
            });
            array.Dispose();
        }

        [Test]
        public void EnumeratesSameOrderAsManaged2DArray()
        {
            const int width = 100;
            const int height = 100;
            float[,] managedArray = new float[width, height];
            int i = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    managedArray[x, y] = i++;
                }
            }

            i = 0;
            foreach (var item in managedArray)
            {
                Assert.AreEqual(i++, item);
            }

            i = 0;
            NativeArray2D<float> nativeArray = new NativeArray2D<float>(managedArray, Unity.Collections.Allocator.Temp);
            foreach (var item in nativeArray)
            {
                Assert.AreEqual(i++, item);
            }
        }

        [Test]
        public void CopyToNativeArray2D()
        {
            var src = new NativeArray2D<int>(10, 3, Unity.Collections.Allocator.Temp);

            int count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    src[x, y] = count++;
                }
            }
            var dst = new NativeArray2D<int>(10, 3, Unity.Collections.Allocator.Temp);
            src.CopyTo(dst);

            count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Assert.AreEqual(count++, dst[x, y]);
                    Assert.AreEqual(src[x, y], dst[x, y]);
                }
            }

            Assert.AreEqual(src.Length0, dst.Length0);
            Assert.AreEqual(src.Length1, dst.Length1);

            src.Dispose();
            dst.Dispose();
        }

        [Test]
        public void CopyFromNativeArray2D()
        {
            var src = new NativeArray2D<int>(10, 3, Unity.Collections.Allocator.Temp);

            int count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    src[x, y] = count++;
                }
            }
            var dst = new NativeArray2D<int>(10, 3, Unity.Collections.Allocator.Temp);
            dst.CopyFrom(src);

            count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Assert.AreEqual(count++, dst[x, y]);
                    Assert.AreEqual(src[x, y], dst[x, y]);
                }
            }

            Assert.AreEqual(src.Length0, dst.Length0);
            Assert.AreEqual(src.Length1, dst.Length1);

            src.Dispose();
            dst.Dispose();
        }

        [Test]
        public void CopyFromArray2D()
        {
            var src = new int[10, 3];

            int count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    src[x, y] = count++;
                }
            }
            var dst = new NativeArray2D<int>(10, 3, Unity.Collections.Allocator.Temp);
            dst.CopyFrom(src);

            count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Assert.AreEqual(count++, dst[x, y]);
                    Assert.AreEqual(src[x, y], dst[x, y]);
                }
            }

            Assert.AreEqual(src.GetLength(0), dst.Length0);
            Assert.AreEqual(src.GetLength(1), dst.Length1);

            dst.Dispose();
        }

        [Test]
        public void CopyToArray2D()
        {
            var src = new NativeArray2D<int>(10, 3, Unity.Collections.Allocator.Temp);

            int count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    src[x, y] = count++;
                }
            }
            var dst = new int[10, 3];
            src.CopyTo(dst);

            count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Assert.AreEqual(count++, dst[x, y]);
                    Assert.AreEqual(src[x, y], dst[x, y]);
                }
            }

            Assert.AreEqual(src.Length0, dst.GetLength(0));
            Assert.AreEqual(src.Length1, dst.GetLength(1));

            src.Dispose();
        }

        [Test]
        public void ToArray()
        {
            var src = new NativeArray2D<int>(10, 3, Unity.Collections.Allocator.Temp);

            int count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    src[x, y] = count++;
                }
            }

            var array = src.ToArray();
            count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Assert.AreEqual(array[x, y], src[x, y]);
                    Assert.AreEqual(array[x, y], count++);
                }
            }

            src.Dispose();
        }

        [Test]
        public void CreateFromNativeArray()
        {
            var src = new NativeArray2D<int>(10, 3, Unity.Collections.Allocator.Temp);

            int count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    src[x, y] = count++;
                }
            }

            var dst = new NativeArray2D<int>(src, Unity.Collections.Allocator.Temp);
            count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Assert.AreEqual(dst[x, y], src[x, y]);
                    Assert.AreEqual(dst[x, y], count++);
                }
            }

            src.Dispose();
            dst.Dispose();
        }

        [Test]
        public void CreateFromArray()
        {
            var src = new int[10, 3];

            int count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    src[x, y] = count++;
                }
            }

            var dst = new NativeArray2D<int>(src, Unity.Collections.Allocator.Temp);
            count = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Assert.AreEqual(dst[x, y], src[x, y]);
                    Assert.AreEqual(dst[x, y], count++);
                }
            }

            dst.Dispose();
        }

        public struct ManagedStructTest
        {
            public string managedVar;
        }

        [Test]
        public void ThrowsIfTypeIsNotUnmanaged()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                NativeArray2D<ManagedStructTest> array = new NativeArray2D<ManagedStructTest>(10, 10, Unity.Collections.Allocator.Temp);

                array.Dispose();
            });
        }

        [Test]
        public void FlattensToManagedArray()
        {
            var nativeArray2D = new NativeArray2D<float>(10, 10, Unity.Collections.Allocator.Temp);
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    nativeArray2D[x, y] = x;
                }
            }

            var flattenedArray = nativeArray2D.Flatten();

            Assert.AreEqual(nativeArray2D.Length, flattenedArray.Length);
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    Assert.AreEqual(flattenedArray[x * 10 + y], nativeArray2D[x, y]);
                }
            }
        }

        [Test]
        public void FlattensToNativeArray()
        {
            var nativeArray2D = new NativeArray2D<float>(10, 10, Unity.Collections.Allocator.Temp);
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    nativeArray2D[x, y] = x;
                }
            }

            var flattenedArray = nativeArray2D.Flatten(Unity.Collections.Allocator.Temp);

            Assert.AreEqual(nativeArray2D.Length, flattenedArray.Length);
            int i = 0;
            foreach (var item in nativeArray2D)
            {
                Assert.AreEqual(flattenedArray[i++], item);
            }
        }
    }
}
