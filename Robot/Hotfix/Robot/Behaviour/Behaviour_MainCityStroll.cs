
using System.Collections.Generic;
using UnityEngine;

namespace ET
{

    //闲逛
    public class Behaviour_MainCityStroll : BehaviourHandler
    {
        public override int BehaviourId()
        {
            return BehaviourType.Behaviour_MainCityStroll;
        }

        public override bool Check(BehaviourComponent aiComponent, AIConfig aiConfig)
        {
            return aiComponent.NewBehaviour == BehaviourId();
        }

        public override async ETTask Execute(BehaviourComponent aiComponent, AIConfig aiConfig, ETCancellationToken cancellationToken)
        {
            int number = 10000;
            Scene zoneScene = aiComponent.ZoneScene();
            Unit myUnit = UnitHelper.GetMyUnitFromZoneScene(zoneScene);
            //await zoneScene.GetComponent<BagComponent>().CheckEquipList();
            //Log.Debug("Behaviour_Stroll: Enter");

            SceneConfig sceneConfig = SceneConfigCategory.Instance.Get(101);
            
            Vector3 initvector3  = (new Vector3()
            {
                x = sceneConfig.InitPos[0] * 0.01f + RandomHelper.RandomNumberFloat(-6f, 6f),
                y = sceneConfig.InitPos[1] * 0.01f,
                z = sceneConfig.InitPos[2] * 0.01f + RandomHelper.RandomNumberFloat(-6f, 6f),
            });
            
            while (number > 0)
            {
                if (myUnit == null || myUnit.IsDisposed)
                {
                    return;
                }

                Vector3 toposition;

                if (RandomHelper.RandFloat01() < 0.5f)
                {
                    List<int> allnpc = SceneConfigCategory.Instance.NpcIdList;
                    int npcid = allnpc[RandomHelper.RandomNumber(0, allnpc.Count)];
                    NpcConfig npcConfig = NpcConfigCategory.Instance.Get(npcid);
                
                    toposition = (new Vector3()
                    {
                        x = npcConfig.Position[0] * 0.01f,
                        y = npcConfig.Position[1] * 0.01f,
                        z = npcConfig.Position[2] * 0.01f,
                    });
  
                    Quaternion rotation = Quaternion.Euler(0, npcConfig.Rotation, 0);
                    toposition += (rotation * Vector3.forward * 3f);
                }
                else
                {
                    initvector3  = (new Vector3()
                    {
                        x = sceneConfig.InitPos[0] * 0.01f + RandomHelper.RandomNumberFloat(-6f, 6f),
                        y = sceneConfig.InitPos[1] * 0.01f,
                        z = sceneConfig.InitPos[2] * 0.01f + RandomHelper.RandomNumberFloat(-6f, 6f),
                    });
                    toposition = initvector3;
                }

                if (Vector3.Distance(myUnit.Position, toposition) > 1f)
                {
                    myUnit.MoveToAsync2(toposition).Coroutine();
                    //Log.Debug($"Behaviour_Stroll: {npcConfig.Name}");
                }
                

                // 因为协程可能被中断，任何协程都要传入cancellationToken，判断如果是中断则要返回
              
                bool timeRet = await TimerComponent.Instance.WaitAsync(TimeHelper.Minute*2, cancellationToken);
                //bool timeRet = await TimerComponent.Instance.WaitAsync(1000, cancellationToken);
                if (!timeRet)
                {
                    return;
                }

                number--;
            }
        }
    }
}
