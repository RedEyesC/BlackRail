using GameFramework.Common;
using GameFramework.Moudule;
using GameFramework.Scene;


namespace GameLogic
{
    internal class GamePoolCtrl : BaseModule
    {

        public static CollectPool<Monster> monsterPool;

        public GamePoolCtrl()
        {
            monsterPool = new CollectPool<Monster>(
            "monsterPool",
            () =>{
                return new Monster();
            },
            (Monster obj) =>{
                obj.Destroy();
            }, 
            (Monster obj) =>{
                obj.Rest();
            });
        }
    }
}
