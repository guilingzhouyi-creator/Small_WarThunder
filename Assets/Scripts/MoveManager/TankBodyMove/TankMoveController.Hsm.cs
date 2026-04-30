using UnityEngine;

public partial class TankMoveController : MonoBehaviour
{
    private enum TravelState
    {
        Idle,
        Forward,
        Reverse
    }

    private enum SteeringRegime
    {
        Idle,
        Straight,
        PivotTurn,
        MovingTurn,
        BrakeTurn
    }

    private readonly struct MovementInputSnapshot
    {
        public MovementInputSnapshot(float forwardInput, float steerInput)
        {
            ForwardInput = forwardInput;
            SteerInput = steerInput;
        }

        public float ForwardInput { get; }
        public float SteerInput { get; }

        public bool HasTravelCommand => Mathf.Abs(ForwardInput) > 0.001f;
        public bool HasSteeringCommand => Mathf.Abs(SteerInput) > 0.001f;
    }

    private readonly struct MovementHsmState
    {
        public MovementHsmState(MovementInputSnapshot input, TravelState travelState, SteeringRegime steeringRegime)
        {
            Input = input;
            CurrentTravelState = travelState;
            CurrentSteeringRegime = steeringRegime;
        }

        public MovementInputSnapshot Input { get; }
        public TravelState CurrentTravelState { get; }
        public SteeringRegime CurrentSteeringRegime { get; }

        public bool HasTravelCommand => Input.HasTravelCommand;
        public bool HasSteeringCommand => Input.HasSteeringCommand;
        public bool HasLatchedTravel => CurrentTravelState != TravelState.Idle;

        public float TravelInputCommand => Input.ForwardInput;
        public float TravelCommandSign => Mathf.Sign(TravelInputCommand);
        public float RawSteerInput => Input.SteerInput;

        public float TrackTurnInput
        {
            get
            {
                if (!HasSteeringCommand)
                {
                    return 0f;
                }

                return RawSteerInput;
            }
        }

        public float YawTurnInput
        {
            get
            {
                if (!HasSteeringCommand)
                {
                    return 0f;
                }

                if (HasTravelCommand && CurrentTravelState == TravelState.Reverse)
                {
                    return -RawSteerInput;
                }

                return RawSteerInput;
            }
        }
    }

    private const float TravelIdleReleaseSpeed = 0.2f;
    private const float TravelLatchReleaseSpeed = 0.6f;

    private TravelState _latchedTravelState = TravelState.Idle;

    private MovementHsmState ResolveMovementHsmState(float currentForwardSpeed, float speedAbs)
    {
        MovementInputSnapshot input = new MovementInputSnapshot(GetForwardInput(), GetTurnInput());
        TravelState travelState = ResolveTravelState(input, currentForwardSpeed, speedAbs);
        SteeringRegime steeringRegime = ResolveSteeringRegime(input, speedAbs);
        return new MovementHsmState(input, travelState, steeringRegime);
    }

    private TravelState ResolveTravelState(MovementInputSnapshot input, float currentForwardSpeed, float speedAbs)
    {
        if (input.ForwardInput > 0.001f)
        {
            _latchedTravelState = TravelState.Forward;
            return _latchedTravelState;
        }

        if (input.ForwardInput < -0.001f)
        {
            _latchedTravelState = TravelState.Reverse;
            return _latchedTravelState;
        }

        if (speedAbs <= TravelIdleReleaseSpeed)
        {
            _latchedTravelState = TravelState.Idle;
            return _latchedTravelState;
        }

        if (_latchedTravelState != TravelState.Idle && speedAbs >= TravelLatchReleaseSpeed)
        {
            return _latchedTravelState;
        }

        _latchedTravelState = currentForwardSpeed >= 0f ? TravelState.Forward : TravelState.Reverse;
        return _latchedTravelState;
    }

    private SteeringRegime ResolveSteeringRegime(MovementInputSnapshot input, float speedAbs)
    {
        if (!input.HasTravelCommand && !input.HasSteeringCommand)
        {
            return SteeringRegime.Idle;
        }

        if (input.HasSteeringCommand && !input.HasTravelCommand)
        {
            return SteeringRegime.PivotTurn;
        }

        if (input.HasSteeringCommand)
        {
            return speedAbs <= GetBrakeTurnMaxSpeed() ? SteeringRegime.BrakeTurn : SteeringRegime.MovingTurn;
        }

        return SteeringRegime.Straight;
    }

    private float ResolveTravelSign(TravelState travelState, float fallbackSpeed)
    {
        if (travelState == TravelState.Forward)
        {
            return 1f;
        }

        if (travelState == TravelState.Reverse)
        {
            return -1f;
        }

        float travelSign = Mathf.Sign(fallbackSpeed);
        if (Mathf.Abs(travelSign) < 0.001f)
        {
            travelSign = 1f;
        }

        return travelSign;
    }
}