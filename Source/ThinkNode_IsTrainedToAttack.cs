using Verse;
using Verse.AI;

namespace DraftableAnimals
{
    public class ThinkNode_IsTrainedToAttack : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return DraftableAnimalsUtility.IsTrainedToAttack(pawn);
        }
    }
}