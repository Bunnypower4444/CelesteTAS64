
namespace Celeste64.TAS;

public struct Vec2Polar
{
    private float angle;
    private float length;
    public float Angle
    {
        readonly get => angle;
        set { angle = value; CorrectAngle(); }
    }
    public float Length
    {
        readonly get => length;
        set
        {
            length = value;
            if (length < 0)
            {
                length *= -1;
                angle += MathF.PI;
                CorrectAngle();
            }
        }
    }

    public Vec2Polar(float angle, float length)
    {
        Angle = angle;
        Length = length;
    }

    public Vec2Polar(Vec2 vector)
        : this(vector.Angle(), vector.Length()) {}

    private void CorrectAngle()
    {
        angle %= MathF.Tau;
        if (angle > MathF.PI || angle <= -MathF.PI)
            angle -= MathF.Sign(angle) * MathF.Tau;
    }

    public readonly Vec2Polar Normalized()
        => this with { Length = MathF.Sign(Length) };

    public readonly Vec2Polar Add(Vec2Polar other)
    {
        // figures out the angle of the new vector, then projects each vector onto that line to find the length
        var newAngle = ((Vec2)this + (Vec2)other).Angle();
        return new(newAngle, MathF.Cos(newAngle - Angle) * Length + MathF.Cos(other.Angle - newAngle) * other.Length);
    }
    
    public readonly Vec2Polar Multiply(float scalar)
        => this with { Length = Length * scalar };

    public readonly float X => MathF.Cos(Angle) * Length;
    public readonly float Y => MathF.Sin(Angle) * Length;

    public static Vec2Polar operator +(Vec2Polar a, Vec2Polar b)
        => a.Add(b);

    public static Vec2Polar operator *(Vec2Polar vec, float scalar)
        => vec.Multiply(scalar);

    public static implicit operator Vec2(Vec2Polar polar)
        => new(polar.X, polar.Y);
    
    public static implicit operator Vec2Polar(Vec2 xy)
        => new(xy);
}