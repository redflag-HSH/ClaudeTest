using UnityEngine;
using UnityEngine.Rendering;

public class StateMachine : MonoBehaviour
{
    BaseState _currentState;

    void Update()
    {
        if (_currentState != null)
            _currentState.Perform();
    }
    public void ChangeState(BaseState newState)
    {
        //check activeState 
        if (_currentState != null)
        {
            //이전 스테이트 정리
            _currentState.Exit();
        }
        //새로운 스테이트 진입
        _currentState = newState;

        //페일 세이프:제대로 스테이트가 들어갔는지 확인
        if (_currentState != null)
        {
            //새 스테이트가 자리잡게 세팅
            _currentState.StateMachine = this;
            _currentState.Enter();
        }
    }
}
