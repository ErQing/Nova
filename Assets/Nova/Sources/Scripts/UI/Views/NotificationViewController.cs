﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class NotificationViewController : ViewControllerBase
    {
        public GameObject notificationPrefab;
        public float notificationTimePerChar = 0.1f;
        public float notificationTimeOffset = 1f;
        public float notificationDropSpeed = 500f;

        private float placeholdingDeadItemCountPercentage
        {
            get => (layoutGroupBasePos.y - layoutGroupTransform.position.y) * (RealScreen.uiSize.y / UICameraHelper.Active.orthographicSize * 0.5f);
            set
            {
                Vector3 newPos = layoutGroupTransform.position;
                newPos.y = layoutGroupBasePos.y - value / (RealScreen.uiSize.y / UICameraHelper.Active.orthographicSize * 0.5f);
                layoutGroupTransform.position = newPos;
            }
        }

        private Vector2 layoutGroupBasePos;
        private RectTransform layoutGroupTransform;

        protected override void Awake()
        {
            base.Awake();
            this.RuntimeAssert(notificationPrefab != null, "Missing NotificationPrefab.");
        }

        protected override void Start()
        {
            base.Start();
            myPanel.SetActive(true);
            layoutGroupTransform = myPanel.GetComponent<RectTransform>();
            layoutGroupBasePos = layoutGroupTransform.position;
        }

        private IEnumerator NotificationFadeOut(GameObject notification, float timeout, Action onFinish)
        {
            yield return new WaitForSeconds(timeout);
            var transition = notification.GetComponent<UIViewTransitionBase>();

            // TODO: there is a NovaAnimation in NotificationItem, and this Action is in that NovaAnimation.
            // Destroy(notification) will call NovaAnimation.OnDisable() -> NovaAnimation.Stop() -> this Action.
            // That is a strange call stack.
            bool onFinishInvoked = false;
            transition.ResetTransitionTarget();
            transition.Exit(() =>
            {
                if (onFinishInvoked) return;
                onFinishInvoked = true;
                onFinish?.Invoke();

                placeholdingDeadItemCountPercentage += notification.GetComponent<RectTransform>().rect.size.y;
                Destroy(notification);
            });
        }

        public void Notify(AlertParameters param)
        {
            if (!param.lite)
            {
                return;
            }

            var notification = Instantiate(notificationPrefab, myPanel.transform);
            notification.SetActive(true);
            notification.GetComponentInChildren<Text>().text = param.bodyContent;
            float timeout = notificationTimePerChar * param.bodyContent.Length + notificationTimeOffset;
            ForceRebuildLayoutAndResetTransitionTarget();
            var transition = notification.GetComponent<UIViewTransitionBase>();
            transition.Enter(() => StartCoroutine(NotificationFadeOut(notification, timeout, param.onCancel)));
        }

        protected override void Update()
        {
            var delta = placeholdingDeadItemCountPercentage;
            if (delta < float.Epsilon)
            {
                placeholdingDeadItemCountPercentage = 0f;
            }
            else
            {
                placeholdingDeadItemCountPercentage = delta - Time.deltaTime * notificationDropSpeed;
            }
        }
    }
}