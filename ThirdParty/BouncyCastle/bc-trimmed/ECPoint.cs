//*********************************************************
//
// This file was imported from the C# Bouncy Castle project. Original license header is retained:
//
//
// License
// Copyright (c) 2000-2014 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
//
//*********************************************************

using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace BouncyCastle
{
    /**
     * base class for points on elliptic curves.
     */
    public abstract class ECPoint
    {
        protected static ECFieldElement[] EMPTY_ZS = new ECFieldElement[0];

        protected static ECFieldElement[] GetInitialZCoords(ECCurve curve)
        {
            // Cope with null curve, most commonly used by implicitlyCa
            int coord = null == curve ? ECCurve.COORD_AFFINE : curve.CoordinateSystem;

            switch (coord)
            {
                case ECCurve.COORD_AFFINE:
                case ECCurve.COORD_LAMBDA_AFFINE:
                    return EMPTY_ZS;
                default:
                    break;
            }

            ECFieldElement one = curve.FromBigInteger(BigInteger.One);

            switch (coord)
            {
                case ECCurve.COORD_HOMOGENEOUS:
                case ECCurve.COORD_JACOBIAN:
                case ECCurve.COORD_LAMBDA_PROJECTIVE:
                    return new ECFieldElement[] { one };
                case ECCurve.COORD_JACOBIAN_CHUDNOVSKY:
                    return new ECFieldElement[] { one, one, one };
                case ECCurve.COORD_JACOBIAN_MODIFIED:
                    return new ECFieldElement[] { one, curve.A };
                default:
                    throw new ArgumentException("unknown coordinate system");
            }
        }

        protected internal readonly ECCurve m_curve;
        protected internal readonly ECFieldElement m_x, m_y;
        protected internal readonly ECFieldElement[] m_zs;
        protected internal readonly bool m_withCompression;

        protected internal PreCompInfo m_preCompInfo = null;

        protected ECPoint(ECCurve curve, ECFieldElement x, ECFieldElement y, bool withCompression)
            : this(curve, x, y, GetInitialZCoords(curve), withCompression)
        {
        }

        internal ECPoint(ECCurve curve, ECFieldElement x, ECFieldElement y, ECFieldElement[] zs, bool withCompression)
        {
            this.m_curve = curve;
            this.m_x = x;
            this.m_y = y;
            this.m_zs = zs;
            this.m_withCompression = withCompression;
        }

        public ECPoint GetDetachedPoint()
        {
            return Normalize().Detach();
        }

        public virtual ECCurve Curve
        {
            get { return m_curve; }
        }

        protected abstract ECPoint Detach();

        protected virtual int CurveCoordinateSystem
        {
            get
            {
                // Cope with null curve, most commonly used by implicitlyCa
                return null == m_curve ? ECCurve.COORD_AFFINE : m_curve.CoordinateSystem;
            }
        }

        ///**
        // * Normalizes this point, and then returns the affine x-coordinate.
        // * 
        // * Note: normalization can be expensive, this method is deprecated in favour
        // * of caller-controlled normalization.
        // */
        //[Obsolete("Use AffineXCoord, or Normalize() and XCoord, instead")]
        //public virtual ECFieldElement X
        //{
        //    get { return Normalize().XCoord; }
        //}

        ///**
        // * Normalizes this point, and then returns the affine y-coordinate.
        // * 
        // * Note: normalization can be expensive, this method is deprecated in favour
        // * of caller-controlled normalization.
        // */
        //[Obsolete("Use AffineYCoord, or Normalize() and YCoord, instead")]
        //public virtual ECFieldElement Y
        //{
        //    get { return Normalize().YCoord; }
        //}

        /**
         * Returns the affine x-coordinate after checking that this point is normalized.
         * 
         * @return The affine x-coordinate of this point
         * @throws IllegalStateException if the point is not normalized
         */
        public virtual ECFieldElement AffineXCoord
        {
            get
            {
                CheckNormalized();
                return XCoord;
            }
        }

        /**
         * Returns the affine y-coordinate after checking that this point is normalized
         * 
         * @return The affine y-coordinate of this point
         * @throws IllegalStateException if the point is not normalized
         */
        public virtual ECFieldElement AffineYCoord
        {
            get
            {
                CheckNormalized();
                return YCoord;
            }
        }

        /**
         * Returns the x-coordinate.
         * 
         * Caution: depending on the curve's coordinate system, this may not be the same value as in an
         * affine coordinate system; use Normalize() to get a point where the coordinates have their
         * affine values, or use AffineXCoord if you expect the point to already have been normalized.
         * 
         * @return the x-coordinate of this point
         */
        public virtual ECFieldElement XCoord
        {
            get { return m_x; }
        }

        /**
         * Returns the y-coordinate.
         * 
         * Caution: depending on the curve's coordinate system, this may not be the same value as in an
         * affine coordinate system; use Normalize() to get a point where the coordinates have their
         * affine values, or use AffineYCoord if you expect the point to already have been normalized.
         * 
         * @return the y-coordinate of this point
         */
        public virtual ECFieldElement YCoord
        {
            get { return m_y; }
        }

        public virtual ECFieldElement GetZCoord(int index)
        {
            return (index < 0 || index >= m_zs.Length) ? null : m_zs[index];
        }

        public virtual ECFieldElement[] GetZCoords()
        {
            int zsLen = m_zs.Length;
            if (zsLen == 0)
            {
                return m_zs;
            }
            ECFieldElement[] copy = new ECFieldElement[zsLen];
            Array.Copy(m_zs, 0, copy, 0, zsLen);
            return copy;
        }

        protected internal ECFieldElement RawXCoord
        {
            get { return m_x; }
        }

        protected internal ECFieldElement RawYCoord
        {
            get { return m_y; }
        }

        protected internal ECFieldElement[] RawZCoords
        {
            get { return m_zs; }
        }

        protected virtual void CheckNormalized()
        {
            if (!IsNormalized())
                throw new InvalidOperationException("point not in normal form");
        }

        public virtual bool IsNormalized()
        {
            int coord = this.CurveCoordinateSystem;

            return coord == ECCurve.COORD_AFFINE
                || coord == ECCurve.COORD_LAMBDA_AFFINE
                || IsInfinity
                || RawZCoords[0].IsOne;
        }

        /**
         * Normalization ensures that any projective coordinate is 1, and therefore that the x, y
         * coordinates reflect those of the equivalent point in an affine coordinate system.
         * 
         * @return a new ECPoint instance representing the same point, but with normalized coordinates
         */
        public virtual ECPoint Normalize()
        {
            if (this.IsInfinity)
            {
                return this;
            }

            switch (this.CurveCoordinateSystem)
            {
                case ECCurve.COORD_AFFINE:
                case ECCurve.COORD_LAMBDA_AFFINE:
                    {
                        return this;
                    }
                default:
                    {
                        ECFieldElement Z1 = RawZCoords[0];
                        if (Z1.IsOne)
                        {
                            return this;
                        }

                        return Normalize(Z1.Invert());
                    }
            }
        }

        internal virtual ECPoint Normalize(ECFieldElement zInv)
        {
            switch (this.CurveCoordinateSystem)
            {
                case ECCurve.COORD_HOMOGENEOUS:
                case ECCurve.COORD_LAMBDA_PROJECTIVE:
                    {
                        return CreateScaledPoint(zInv, zInv);
                    }
                case ECCurve.COORD_JACOBIAN:
                case ECCurve.COORD_JACOBIAN_CHUDNOVSKY:
                case ECCurve.COORD_JACOBIAN_MODIFIED:
                    {
                        ECFieldElement zInv2 = zInv.Square(), zInv3 = zInv2.Multiply(zInv);
                        return CreateScaledPoint(zInv2, zInv3);
                    }
                default:
                    {
                        throw new InvalidOperationException("not a projective coordinate system");
                    }
            }
        }

        protected virtual ECPoint CreateScaledPoint(ECFieldElement sx, ECFieldElement sy)
        {
            return Curve.CreateRawPoint(RawXCoord.Multiply(sx), RawYCoord.Multiply(sy), IsCompressed);
        }

        public bool IsInfinity
        {
            get { return m_x == null && m_y == null; }
        }

        public bool IsCompressed
        {
            get { return m_withCompression; }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ECPoint);
        }

        public virtual bool Equals(ECPoint other)
        {
            if (this == other)
                return true;
            if (null == other)
                return false;

            ECCurve c1 = this.Curve, c2 = other.Curve;
            bool n1 = (null == c1), n2 = (null == c2);
            bool i1 = IsInfinity, i2 = other.IsInfinity;

            if (i1 || i2)
            {
                return (i1 && i2) && (n1 || n2 || c1.Equals(c2));
            }

            ECPoint p1 = this, p2 = other;
            if (n1 && n2)
            {
                // Points with null curve are in affine form, so already normalized
            }
            else if (n1)
            {
                p2 = p2.Normalize();
            }
            else if (n2)
            {
                p1 = p1.Normalize();
            }
            else if (!c1.Equals(c2))
            {
                return false;
            }
            else
            {
                // TODO Consider just requiring already normalized, to avoid silent performance degradation

                ECPoint[] points = new ECPoint[] { this, c1.ImportPoint(p2) };

                // TODO This is a little strong, really only requires coZNormalizeAll to get Zs equal
                c1.NormalizeAll(points);

                p1 = points[0];
                p2 = points[1];
            }

            return p1.XCoord.Equals(p2.XCoord) && p1.YCoord.Equals(p2.YCoord);
        }

        public override int GetHashCode()
        {
            ECCurve c = this.Curve;
            int hc = (null == c) ? 0 : ~c.GetHashCode();

            if (!this.IsInfinity)
            {
                // TODO Consider just requiring already normalized, to avoid silent performance degradation

                ECPoint p = Normalize();

                hc ^= p.XCoord.GetHashCode() * 17;
                hc ^= p.YCoord.GetHashCode() * 257;
            }

            return hc;
        }

        public override string ToString()
        {
            if (this.IsInfinity)
            {
                return "INF";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append('(');
            sb.Append(RawXCoord);
            sb.Append(',');
            sb.Append(RawYCoord);
            for (int i = 0; i < m_zs.Length; ++i)
            {
                sb.Append(',');
                sb.Append(m_zs[i]);
            }
            sb.Append(')');
            return sb.ToString();
        }

        public virtual byte[] GetEncoded()
        {
            return GetEncoded(m_withCompression);
        }

        public abstract byte[] GetEncoded(bool compressed);

        protected internal abstract bool CompressionYTilde { get; }

        public abstract ECPoint Add(ECPoint b);
        public abstract ECPoint Subtract(ECPoint b);
        public abstract ECPoint Negate();

        public virtual ECPoint TimesPow2(int e)
        {
            if (e < 0)
                throw new ArgumentException("cannot be negative", "e");

            ECPoint p = this;
            while (--e >= 0)
            {
                p = p.Twice();
            }
            return p;
        }

        public abstract ECPoint Twice();
        public abstract ECPoint Multiply(BigInteger b);

        public virtual ECPoint TwicePlus(ECPoint b)
        {
            return Twice().Add(b);
        }

        public virtual ECPoint ThreeTimes()
        {
            return TwicePlus(this);
        }
    }

    public abstract class ECPointBase
        : ECPoint
    {
        protected internal ECPointBase(
            ECCurve curve,
            ECFieldElement x,
            ECFieldElement y,
            bool withCompression)
            : base(curve, x, y, withCompression)
        {
        }

        protected internal ECPointBase(ECCurve curve, ECFieldElement x, ECFieldElement y, ECFieldElement[] zs, bool withCompression)
            : base(curve, x, y, zs, withCompression)
        {
        }

        /**
         * return the field element encoded with point compression. (S 4.3.6)
         */
        public override byte[] GetEncoded(bool compressed)
        {
            if (this.IsInfinity)
            {
                return new byte[1];
            }

            ECPoint normed = Normalize();

            byte[] X = normed.XCoord.GetEncoded();

            if (compressed)
            {
                byte[] PO = new byte[X.Length + 1];
                PO[0] = (byte)(normed.CompressionYTilde ? 0x03 : 0x02);
                Array.Copy(X, 0, PO, 1, X.Length);
                return PO;
            }

            byte[] Y = normed.YCoord.GetEncoded();

            {
                byte[] PO = new byte[X.Length + Y.Length + 1];
                PO[0] = 0x04;
                Array.Copy(X, 0, PO, 1, X.Length);
                Array.Copy(Y, 0, PO, X.Length + 1, Y.Length);
                return PO;
            }
        }

        /**
         * Multiplies this <code>ECPoint</code> by the given number.
         * @param k The multiplicator.
         * @return <code>k * this</code>.
         */
        public override ECPoint Multiply(BigInteger k)
        {
            return this.Curve.GetMultiplier().Multiply(this, k);
        }
    }

    /**
     * Elliptic curve points over Fp
     */
    public class FpPoint
        : ECPointBase
    {
        /**
         * Create a point which encodes with point compression.
         *
         * @param curve the curve to use
         * @param x affine x co-ordinate
         * @param y affine y co-ordinate
         */
        public FpPoint(ECCurve curve, ECFieldElement x, ECFieldElement y)
            : this(curve, x, y, false)
        {
        }

        /**
         * Create a point that encodes with or without point compresion.
         *
         * @param curve the curve to use
         * @param x affine x co-ordinate
         * @param y affine y co-ordinate
         * @param withCompression if true encode with point compression
         */
        public FpPoint(ECCurve curve, ECFieldElement x, ECFieldElement y, bool withCompression)
            : base(curve, x, y, withCompression)
        {
            if ((x == null) != (y == null))
                throw new ArgumentException("Exactly one of the field elements is null");
        }

        internal FpPoint(ECCurve curve, ECFieldElement x, ECFieldElement y, ECFieldElement[] zs, bool withCompression)
            : base(curve, x, y, zs, withCompression)
        {
        }

        protected override ECPoint Detach()
        {
            return new FpPoint(null, AffineXCoord, AffineYCoord);
        }

        protected internal override bool CompressionYTilde
        {
            get { return this.AffineYCoord.TestBitZero(); }
        }

        public override ECFieldElement GetZCoord(int index)
        {
            if (index == 1 && ECCurve.COORD_JACOBIAN_MODIFIED == this.CurveCoordinateSystem)
            {
                return GetJacobianModifiedW();
            }

            return base.GetZCoord(index);
        }

        // B.3 pg 62
        public override ECPoint Add(ECPoint b)
        {
            if (this.IsInfinity)
                return b;
            if (b.IsInfinity)
                return this;
            if (this == b)
                return Twice();

            ECCurve curve = this.Curve;
            int coord = curve.CoordinateSystem;

            ECFieldElement X1 = this.RawXCoord, Y1 = this.RawYCoord;
            ECFieldElement X2 = b.RawXCoord, Y2 = b.RawYCoord;

            switch (coord)
            {
                case ECCurve.COORD_AFFINE:
                    {
                        ECFieldElement dx = X2.Subtract(X1), dy = Y2.Subtract(Y1);

                        if (dx.IsZero)
                        {
                            if (dy.IsZero)
                            {
                                // this == b, i.e. this must be doubled
                                return Twice();
                            }

                            // this == -b, i.e. the result is the point at infinity
                            return Curve.Infinity;
                        }

                        ECFieldElement gamma = dy.Divide(dx);
                        ECFieldElement X3 = gamma.Square().Subtract(X1).Subtract(X2);
                        ECFieldElement Y3 = gamma.Multiply(X1.Subtract(X3)).Subtract(Y1);

                        return new FpPoint(Curve, X3, Y3, IsCompressed);
                    }

                case ECCurve.COORD_HOMOGENEOUS:
                    {
                        ECFieldElement Z1 = this.RawZCoords[0];
                        ECFieldElement Z2 = b.RawZCoords[0];

                        bool Z1IsOne = Z1.IsOne;
                        bool Z2IsOne = Z2.IsOne;

                        ECFieldElement u1 = Z1IsOne ? Y2 : Y2.Multiply(Z1);
                        ECFieldElement u2 = Z2IsOne ? Y1 : Y1.Multiply(Z2);
                        ECFieldElement u = u1.Subtract(u2);
                        ECFieldElement v1 = Z1IsOne ? X2 : X2.Multiply(Z1);
                        ECFieldElement v2 = Z2IsOne ? X1 : X1.Multiply(Z2);
                        ECFieldElement v = v1.Subtract(v2);

                        // Check if b == this or b == -this
                        if (v.IsZero)
                        {
                            if (u.IsZero)
                            {
                                // this == b, i.e. this must be doubled
                                return this.Twice();
                            }

                            // this == -b, i.e. the result is the point at infinity
                            return curve.Infinity;
                        }

                        // TODO Optimize for when w == 1
                        ECFieldElement w = Z1IsOne ? Z2 : Z2IsOne ? Z1 : Z1.Multiply(Z2);
                        ECFieldElement vSquared = v.Square();
                        ECFieldElement vCubed = vSquared.Multiply(v);
                        ECFieldElement vSquaredV2 = vSquared.Multiply(v2);
                        ECFieldElement A = u.Square().Multiply(w).Subtract(vCubed).Subtract(Two(vSquaredV2));

                        ECFieldElement X3 = v.Multiply(A);
                        ECFieldElement Y3 = vSquaredV2.Subtract(A).Multiply(u).Subtract(vCubed.Multiply(u2));
                        ECFieldElement Z3 = vCubed.Multiply(w);

                        return new FpPoint(curve, X3, Y3, new ECFieldElement[] { Z3 }, IsCompressed);
                    }

                case ECCurve.COORD_JACOBIAN:
                case ECCurve.COORD_JACOBIAN_MODIFIED:
                    {
                        ECFieldElement Z1 = this.RawZCoords[0];
                        ECFieldElement Z2 = b.RawZCoords[0];

                        bool Z1IsOne = Z1.IsOne;

                        ECFieldElement X3, Y3, Z3, Z3Squared = null;

                        if (!Z1IsOne && Z1.Equals(Z2))
                        {
                            // TODO Make this available as public method coZAdd?

                            ECFieldElement dx = X1.Subtract(X2), dy = Y1.Subtract(Y2);
                            if (dx.IsZero)
                            {
                                if (dy.IsZero)
                                {
                                    return Twice();
                                }
                                return curve.Infinity;
                            }

                            ECFieldElement C = dx.Square();
                            ECFieldElement W1 = X1.Multiply(C), W2 = X2.Multiply(C);
                            ECFieldElement A1 = W1.Subtract(W2).Multiply(Y1);

                            X3 = dy.Square().Subtract(W1).Subtract(W2);
                            Y3 = W1.Subtract(X3).Multiply(dy).Subtract(A1);
                            Z3 = dx;

                            if (Z1IsOne)
                            {
                                Z3Squared = C;
                            }
                            else
                            {
                                Z3 = Z3.Multiply(Z1);
                            }
                        }
                        else
                        {
                            ECFieldElement Z1Squared, U2, S2;
                            if (Z1IsOne)
                            {
                                Z1Squared = Z1; U2 = X2; S2 = Y2;
                            }
                            else
                            {
                                Z1Squared = Z1.Square();
                                U2 = Z1Squared.Multiply(X2);
                                ECFieldElement Z1Cubed = Z1Squared.Multiply(Z1);
                                S2 = Z1Cubed.Multiply(Y2);
                            }

                            bool Z2IsOne = Z2.IsOne;
                            ECFieldElement Z2Squared, U1, S1;
                            if (Z2IsOne)
                            {
                                Z2Squared = Z2; U1 = X1; S1 = Y1;
                            }
                            else
                            {
                                Z2Squared = Z2.Square();
                                U1 = Z2Squared.Multiply(X1);
                                ECFieldElement Z2Cubed = Z2Squared.Multiply(Z2);
                                S1 = Z2Cubed.Multiply(Y1);
                            }

                            ECFieldElement H = U1.Subtract(U2);
                            ECFieldElement R = S1.Subtract(S2);

                            // Check if b == this or b == -this
                            if (H.IsZero)
                            {
                                if (R.IsZero)
                                {
                                    // this == b, i.e. this must be doubled
                                    return this.Twice();
                                }

                                // this == -b, i.e. the result is the point at infinity
                                return curve.Infinity;
                            }

                            ECFieldElement HSquared = H.Square();
                            ECFieldElement G = HSquared.Multiply(H);
                            ECFieldElement V = HSquared.Multiply(U1);

                            X3 = R.Square().Add(G).Subtract(Two(V));
                            Y3 = V.Subtract(X3).Multiply(R).Subtract(S1.Multiply(G));

                            Z3 = H;
                            if (!Z1IsOne)
                            {
                                Z3 = Z3.Multiply(Z1);
                            }
                            if (!Z2IsOne)
                            {
                                Z3 = Z3.Multiply(Z2);
                            }

                            // Alternative calculation of Z3 using fast square
                            //X3 = four(X3);
                            //Y3 = eight(Y3);
                            //Z3 = doubleProductFromSquares(Z1, Z2, Z1Squared, Z2Squared).multiply(H);

                            if (Z3 == H)
                            {
                                Z3Squared = HSquared;
                            }
                        }

                        ECFieldElement[] zs;
                        if (coord == ECCurve.COORD_JACOBIAN_MODIFIED)
                        {
                            // TODO If the result will only be used in a subsequent addition, we don't need W3
                            ECFieldElement W3 = CalculateJacobianModifiedW(Z3, Z3Squared);

                            zs = new ECFieldElement[] { Z3, W3 };
                        }
                        else
                        {
                            zs = new ECFieldElement[] { Z3 };
                        }

                        return new FpPoint(curve, X3, Y3, zs, IsCompressed);
                    }

                default:
                    {
                        throw new InvalidOperationException("unsupported coordinate system");
                    }
            }
        }

        // B.3 pg 62
        public override ECPoint Twice()
        {
            if (this.IsInfinity)
                return this;

            ECCurve curve = this.Curve;

            ECFieldElement Y1 = this.RawYCoord;
            if (Y1.IsZero)
                return curve.Infinity;

            int coord = curve.CoordinateSystem;

            ECFieldElement X1 = this.RawXCoord;

            switch (coord)
            {
                case ECCurve.COORD_AFFINE:
                    {
                        ECFieldElement X1Squared = X1.Square();
                        ECFieldElement gamma = Three(X1Squared).Add(this.Curve.A).Divide(Two(Y1));
                        ECFieldElement X3 = gamma.Square().Subtract(Two(X1));
                        ECFieldElement Y3 = gamma.Multiply(X1.Subtract(X3)).Subtract(Y1);

                        return new FpPoint(Curve, X3, Y3, IsCompressed);
                    }

                case ECCurve.COORD_HOMOGENEOUS:
                    {
                        ECFieldElement Z1 = this.RawZCoords[0];

                        bool Z1IsOne = Z1.IsOne;

                        // TODO Optimize for small negative a4 and -3
                        ECFieldElement w = curve.A;
                        if (!w.IsZero && !Z1IsOne)
                        {
                            w = w.Multiply(Z1.Square());
                        }
                        w = w.Add(Three(X1.Square()));

                        ECFieldElement s = Z1IsOne ? Y1 : Y1.Multiply(Z1);
                        ECFieldElement t = Z1IsOne ? Y1.Square() : s.Multiply(Y1);
                        ECFieldElement B = X1.Multiply(t);
                        ECFieldElement _4B = Four(B);
                        ECFieldElement h = w.Square().Subtract(Two(_4B));

                        ECFieldElement _2s = Two(s);
                        ECFieldElement X3 = h.Multiply(_2s);
                        ECFieldElement _2t = Two(t);
                        ECFieldElement Y3 = _4B.Subtract(h).Multiply(w).Subtract(Two(_2t.Square()));
                        ECFieldElement _4sSquared = Z1IsOne ? Two(_2t) : _2s.Square();
                        ECFieldElement Z3 = Two(_4sSquared).Multiply(s);

                        return new FpPoint(curve, X3, Y3, new ECFieldElement[] { Z3 }, IsCompressed);
                    }

                case ECCurve.COORD_JACOBIAN:
                    {
                        ECFieldElement Z1 = this.RawZCoords[0];

                        bool Z1IsOne = Z1.IsOne;

                        ECFieldElement Y1Squared = Y1.Square();
                        ECFieldElement T = Y1Squared.Square();

                        ECFieldElement a4 = curve.A;
                        ECFieldElement a4Neg = a4.Negate();

                        ECFieldElement M, S;
                        if (a4Neg.ToBigInteger().Equals(BigInteger.ValueOf(3)))
                        {
                            ECFieldElement Z1Squared = Z1IsOne ? Z1 : Z1.Square();
                            M = Three(X1.Add(Z1Squared).Multiply(X1.Subtract(Z1Squared)));
                            S = Four(Y1Squared.Multiply(X1));
                        }
                        else
                        {
                            ECFieldElement X1Squared = X1.Square();
                            M = Three(X1Squared);
                            if (Z1IsOne)
                            {
                                M = M.Add(a4);
                            }
                            else if (!a4.IsZero)
                            {
                                ECFieldElement Z1Squared = Z1IsOne ? Z1 : Z1.Square();
                                ECFieldElement Z1Pow4 = Z1Squared.Square();
                                if (a4Neg.BitLength < a4.BitLength)
                                {
                                    M = M.Subtract(Z1Pow4.Multiply(a4Neg));
                                }
                                else
                                {
                                    M = M.Add(Z1Pow4.Multiply(a4));
                                }
                            }
                            //S = two(doubleProductFromSquares(X1, Y1Squared, X1Squared, T));
                            S = Four(X1.Multiply(Y1Squared));
                        }

                        ECFieldElement X3 = M.Square().Subtract(Two(S));
                        ECFieldElement Y3 = S.Subtract(X3).Multiply(M).Subtract(Eight(T));

                        ECFieldElement Z3 = Two(Y1);
                        if (!Z1IsOne)
                        {
                            Z3 = Z3.Multiply(Z1);
                        }

                        // Alternative calculation of Z3 using fast square
                        //ECFieldElement Z3 = doubleProductFromSquares(Y1, Z1, Y1Squared, Z1Squared);

                        return new FpPoint(curve, X3, Y3, new ECFieldElement[] { Z3 }, IsCompressed);
                    }

                case ECCurve.COORD_JACOBIAN_MODIFIED:
                    {
                        return TwiceJacobianModified(true);
                    }

                default:
                    {
                        throw new InvalidOperationException("unsupported coordinate system");
                    }
            }
        }

        public override ECPoint TwicePlus(ECPoint b)
        {
            if (this == b)
                return ThreeTimes();
            if (this.IsInfinity)
                return b;
            if (b.IsInfinity)
                return Twice();

            ECFieldElement Y1 = this.RawYCoord;
            if (Y1.IsZero)
                return b;

            ECCurve curve = this.Curve;
            int coord = curve.CoordinateSystem;

            switch (coord)
            {
                case ECCurve.COORD_AFFINE:
                    {
                        ECFieldElement X1 = this.RawXCoord;
                        ECFieldElement X2 = b.RawXCoord, Y2 = b.RawYCoord;

                        ECFieldElement dx = X2.Subtract(X1), dy = Y2.Subtract(Y1);

                        if (dx.IsZero)
                        {
                            if (dy.IsZero)
                            {
                                // this == b i.e. the result is 3P
                                return ThreeTimes();
                            }

                            // this == -b, i.e. the result is P
                            return this;
                        }

                        /*
                         * Optimized calculation of 2P + Q, as described in "Trading Inversions for
                         * Multiplications in Elliptic Curve Cryptography", by Ciet, Joye, Lauter, Montgomery.
                         */

                        ECFieldElement X = dx.Square(), Y = dy.Square();
                        ECFieldElement d = X.Multiply(Two(X1).Add(X2)).Subtract(Y);
                        if (d.IsZero)
                        {
                            return Curve.Infinity;
                        }

                        ECFieldElement D = d.Multiply(dx);
                        ECFieldElement I = D.Invert();
                        ECFieldElement L1 = d.Multiply(I).Multiply(dy);
                        ECFieldElement L2 = Two(Y1).Multiply(X).Multiply(dx).Multiply(I).Subtract(L1);
                        ECFieldElement X4 = (L2.Subtract(L1)).Multiply(L1.Add(L2)).Add(X2);
                        ECFieldElement Y4 = (X1.Subtract(X4)).Multiply(L2).Subtract(Y1);

                        return new FpPoint(Curve, X4, Y4, IsCompressed);
                    }
                case ECCurve.COORD_JACOBIAN_MODIFIED:
                    {
                        return TwiceJacobianModified(false).Add(b);
                    }
                default:
                    {
                        return Twice().Add(b);
                    }
            }
        }

        public override ECPoint ThreeTimes()
        {
            if (this.IsInfinity)
                return this;

            ECFieldElement Y1 = this.RawYCoord;
            if (Y1.IsZero)
                return this;

            ECCurve curve = this.Curve;
            int coord = curve.CoordinateSystem;

            switch (coord)
            {
                case ECCurve.COORD_AFFINE:
                    {
                        ECFieldElement X1 = this.RawXCoord;

                        ECFieldElement _2Y1 = Two(Y1);
                        ECFieldElement X = _2Y1.Square();
                        ECFieldElement Z = Three(X1.Square()).Add(Curve.A);
                        ECFieldElement Y = Z.Square();

                        ECFieldElement d = Three(X1).Multiply(X).Subtract(Y);
                        if (d.IsZero)
                        {
                            return Curve.Infinity;
                        }

                        ECFieldElement D = d.Multiply(_2Y1);
                        ECFieldElement I = D.Invert();
                        ECFieldElement L1 = d.Multiply(I).Multiply(Z);
                        ECFieldElement L2 = X.Square().Multiply(I).Subtract(L1);

                        ECFieldElement X4 = (L2.Subtract(L1)).Multiply(L1.Add(L2)).Add(X1);
                        ECFieldElement Y4 = (X1.Subtract(X4)).Multiply(L2).Subtract(Y1);
                        return new FpPoint(Curve, X4, Y4, IsCompressed);
                    }
                case ECCurve.COORD_JACOBIAN_MODIFIED:
                    {
                        return TwiceJacobianModified(false).Add(this);
                    }
                default:
                    {
                        // NOTE: Be careful about recursions between TwicePlus and ThreeTimes
                        return Twice().Add(this);
                    }
            }
        }

        protected virtual ECFieldElement Two(ECFieldElement x)
        {
            return x.Add(x);
        }

        protected virtual ECFieldElement Three(ECFieldElement x)
        {
            return Two(x).Add(x);
        }

        protected virtual ECFieldElement Four(ECFieldElement x)
        {
            return Two(Two(x));
        }

        protected virtual ECFieldElement Eight(ECFieldElement x)
        {
            return Four(Two(x));
        }

        protected virtual ECFieldElement DoubleProductFromSquares(ECFieldElement a, ECFieldElement b,
            ECFieldElement aSquared, ECFieldElement bSquared)
        {
            /*
             * NOTE: If squaring in the field is faster than multiplication, then this is a quicker
             * way to calculate 2.A.B, if A^2 and B^2 are already known.
             */
            return a.Add(b).Square().Subtract(aSquared).Subtract(bSquared);
        }

        // D.3.2 pg 102 (see Note:)
        public override ECPoint Subtract(
            ECPoint b)
        {
            if (b.IsInfinity)
                return this;

            // Add -b
            return Add(b.Negate());
        }

        public override ECPoint Negate()
        {
            if (IsInfinity)
                return this;

            ECCurve curve = Curve;
            int coord = curve.CoordinateSystem;

            if (ECCurve.COORD_AFFINE != coord)
            {
                return new FpPoint(curve, RawXCoord, RawYCoord.Negate(), RawZCoords, IsCompressed);
            }

            return new FpPoint(curve, RawXCoord, RawYCoord.Negate(), IsCompressed);
        }

        protected virtual ECFieldElement CalculateJacobianModifiedW(ECFieldElement Z, ECFieldElement ZSquared)
        {
            ECFieldElement a4 = this.Curve.A;
            if (a4.IsZero)
                return a4;

            if (ZSquared == null)
            {
                ZSquared = Z.Square();
            }

            ECFieldElement W = ZSquared.Square();
            ECFieldElement a4Neg = a4.Negate();
            if (a4Neg.BitLength < a4.BitLength)
            {
                W = W.Multiply(a4Neg).Negate();
            }
            else
            {
                W = W.Multiply(a4);
            }
            return W;
        }

        protected virtual ECFieldElement GetJacobianModifiedW()
        {
            ECFieldElement[] ZZ = this.RawZCoords;
            ECFieldElement W = ZZ[1];
            if (W == null)
            {
                // NOTE: Rarely, twicePlus will result in the need for a lazy W1 calculation here
                ZZ[1] = W = CalculateJacobianModifiedW(ZZ[0], null);
            }
            return W;
        }

        protected FpPoint TwiceJacobianModified(bool calculateW)
        {
            ECFieldElement X1 = this.RawXCoord, Y1 = this.RawYCoord, Z1 = this.RawZCoords[0], W1 = GetJacobianModifiedW();

            ECFieldElement X1Squared = X1.Square();
            ECFieldElement M = Three(X1Squared).Add(W1);
            ECFieldElement _2Y1 = Two(Y1);
            ECFieldElement _2Y1Squared = _2Y1.Multiply(Y1);
            ECFieldElement S = Two(X1.Multiply(_2Y1Squared));
            ECFieldElement X3 = M.Square().Subtract(Two(S));
            ECFieldElement _4T = _2Y1Squared.Square();
            ECFieldElement _8T = Two(_4T);
            ECFieldElement Y3 = M.Multiply(S.Subtract(X3)).Subtract(_8T);
            ECFieldElement W3 = calculateW ? Two(_8T.Multiply(W1)) : null;
            ECFieldElement Z3 = Z1.IsOne ? _2Y1 : _2Y1.Multiply(Z1);

            return new FpPoint(this.Curve, X3, Y3, new ECFieldElement[] { Z3, W3 }, IsCompressed);
        }
    }
}
