using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CompletedAssets {

    /// <summary>
    /// Rigidbodyの速度を保存しておくクラス
    /// </summary>

    public class RigidbodyVelocity {
        public Vector3 velocity;
        public Vector3 angularVeloccity;
        public RigidbodyVelocity (Rigidbody rigidbody) {
            velocity = rigidbody.velocity;
            angularVeloccity = rigidbody.angularVelocity;
        }
    }

    public class Pausable : MonoBehaviour {

        /// <summary>
        /// 現在Pause中か？
        /// </summary>
        public bool pausing;

        /// <summary>
        /// 無視するGameObject
        /// </summary>
        public GameObject[] ignoreGameObjects;

        /// <summary>
        /// ポーズ状態が変更された瞬間を調べるため、前回のポーズ状況を記録しておく
        /// </summary>
        bool prevPausing;

        /// <summary>
        /// Rigidbodyのポーズ前の速度の配列
        /// </summary>
        RigidbodyVelocity[] rigidbodyVelocities;

        /// <summary>
        /// ポーズ中のRigidbodyの配列
        /// </summary>
        Rigidbody[] pausingRigidbodies;

        /// <summary>
        /// ポーズ中のMonoBehaviourの配列
        /// </summary>
        MonoBehaviour[] pausingMonoBehaviours;

        [SerializeField]
        private GameObject pauseUIPrefab;
        //　ポーズUIのインスタンス
        private GameObject pauseUIInstance;

        /// <summary>
        /// 更新処理
        /// </summary>
        void Update () {
            if (Input.GetKeyDown (KeyCode.Q)) {
                pausing = !pausing;
            }
            // ポーズ状態が変更されていたら、Pause/Resumeを呼び出す。
            if (prevPausing != pausing) {
                if (pausing) Pause ();
                else Resume ();
                prevPausing = pausing;
            }
        }

        /// <summary>
        /// 中断
        /// </summary>
        void Pause () {
            GameObject.FindGameObjectWithTag ("PlayerManager").GetComponent<PlayerManager> ().canBomb = false;
            // Rigidbodyの停止
            // 子要素から、スリープ中でなく、IgnoreGameObjectsに含まれていないRigidbodyを抽出
            Predicate<Rigidbody> rigidbodyPredicate =
                obj => !obj.IsSleeping () &&
                Array.FindIndex (ignoreGameObjects, gameObject => gameObject == obj.gameObject) < 0;
            pausingRigidbodies = Array.FindAll (transform.GetComponentsInChildren<Rigidbody> (), rigidbodyPredicate);
            rigidbodyVelocities = new RigidbodyVelocity[pausingRigidbodies.Length];
            for (int i = 0; i < pausingRigidbodies.Length; i++) {
                // 速度、角速度も保存しておく
                rigidbodyVelocities[i] = new RigidbodyVelocity (pausingRigidbodies[i]);
                pausingRigidbodies[i].Sleep ();
            }

            // MonoBehaviourの停止
            // 子要素から、有効かつこのインスタンスでないもの、IgnoreGameObjectsに含まれていないMonoBehaviourを抽出
            Predicate<MonoBehaviour> monoBehaviourPredicate =
                obj => obj.enabled &&
                obj != this &&
                Array.FindIndex (ignoreGameObjects, gameObject => gameObject == obj.gameObject) < 0;
            pausingMonoBehaviours = Array.FindAll (transform.GetComponentsInChildren<MonoBehaviour> (), monoBehaviourPredicate);
            foreach (var monoBehaviour in pausingMonoBehaviours) {
                monoBehaviour.enabled = false;
            }

            pauseUIInstance = GameObject.Instantiate (pauseUIPrefab) as GameObject;
            Time.timeScale = 0f;

        }

        /// <summary>
        /// 再開
        /// </summary>
        void Resume () {
            // Rigidbodyの再開
            for (int i = 0; i < pausingRigidbodies.Length; i++) {
                pausingRigidbodies[i].WakeUp ();
                pausingRigidbodies[i].velocity = rigidbodyVelocities[i].velocity;
                pausingRigidbodies[i].angularVelocity = rigidbodyVelocities[i].angularVeloccity;
            }

            // MonoBehaviourの再開
            foreach (var monoBehaviour in pausingMonoBehaviours) {
                if (monoBehaviour != null) {
                    monoBehaviour.enabled = true;
                }
            }

            Destroy (pauseUIInstance);
            Time.timeScale = 1f;
            GameObject.FindGameObjectWithTag ("PlayerManager").GetComponent<PlayerManager> ().canBomb = true;
            SubjectProvider.pauseEnd.OnNext (true);
        }

    }
}