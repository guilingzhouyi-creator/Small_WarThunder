using UnityEngine;

public partial class TankMoveController : MonoBehaviour
{
    private float GetForwardInput()
    {
        if (!CanAcceptPropulsionInput)
        {
            return 0f;
        }

        bool isForwardPressed = MIddleInputingController.Instance.IsForwardPressed();
        bool isBackwardPressed = MIddleInputingController.Instance.IsBackwardPressed();

        if (isForwardPressed == isBackwardPressed)
        {
            return 0f;
        }

        return isForwardPressed ? 1f : -1f;
    }

    private float GetTurnInput()
    {
        if (!CanAcceptPropulsionInput)
        {
            return 0f;
        }

        bool isTurningLeftPressed = MIddleInputingController.Instance.IsTurningLeftPressed();
        bool isTurningRightPressed = MIddleInputingController.Instance.IsTurningRightPressed();

        if (isTurningLeftPressed == isTurningRightPressed)
        {
            return 0f;
        }

        // 这里仅保留玩家输入语义：A=-1，D=1。
        // 前进/后退时的物理解算镜像在 Motion 层处理，避免输入层和物理层职责混杂。
        return isTurningLeftPressed ? -1f : 1f;
    }
}
