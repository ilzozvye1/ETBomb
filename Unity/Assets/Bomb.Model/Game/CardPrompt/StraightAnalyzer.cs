﻿using System.Collections.Generic;

namespace Bomb.CardPrompt
{
    public class StraightAnalyzer: IAnalyzer
    {
        public bool Check(CardType targetType)
        {
            return targetType == CardType.Straight;
        }

        public void Invoke(AnalysisContext context)
        {
            for (int i = 0; i <= context.AnalyseResults.Count - context.Target.Count; i++)
            {
                List<Card> tmpCards = new List<Card>();
                for (int j = i; j < context.Target.Count + i; j++)
                {
                    tmpCards.Add(context.AnalyseResults[j].Cards[0]);
                }

                if (context.CheckPop(tmpCards))
                {
                    context.Add(tmpCards);
                }
            }
        }
    }
}