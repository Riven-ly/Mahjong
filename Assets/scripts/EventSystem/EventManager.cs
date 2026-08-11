using System;
using System.Collections.Generic;
using UnityEngine;

// 监听器接口
public interface IEventListener
{
    // 处理事件的方法
    public void OnEventTriggered(GameEvent eventType, object data = null);
}

// 事件管理器（单例模式）
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }
    private Dictionary<GameEvent, List<IEventListener>> _eventListeners = new Dictionary<GameEvent, List<IEventListener>>();

    private void Awake()
    {
        Instance = this;
    }

    // 注册监听器
    public void RegisterListener(GameEvent eventType, IEventListener listener)
    {
        // 如果字典中没有该事件类型的列表，则创建一个
        if (!_eventListeners.ContainsKey(eventType))
        {
            _eventListeners[eventType] = new List<IEventListener>();
        }

        // 如果监听器不在列表中，则添加
        if (!_eventListeners[eventType].Contains(listener))
        {
            _eventListeners[eventType].Add(listener);
        }
    }

    // 注销监听器
    public void UnregisterListener(GameEvent eventType, IEventListener listener)
    {
        // 如果字典中存在该事件类型的列表，则尝试移除监听器
        if (_eventListeners.ContainsKey(eventType) && _eventListeners[eventType].Contains(listener))
        {
            _eventListeners[eventType].Remove(listener);

            // 如果列表为空，移除该事件类型的键
            if (_eventListeners[eventType].Count == 0)
            {
                _eventListeners.Remove(eventType);
            }
        }
    }

    // 触发事件
    public void TriggerEvent(GameEvent eventType, object data = null)
    {
        // 如果有监听器监听该事件，则通知所有监听器
        if (_eventListeners.TryGetValue(eventType, out var listeners))
        {
            // 创建一个临时列表，防止在遍历过程中修改原列表
            List<IEventListener> tempListeners = new List<IEventListener>(listeners);

            foreach (var listener in tempListeners)
            {
                listener.OnEventTriggered(eventType, data);
            }
        }
    }
}
