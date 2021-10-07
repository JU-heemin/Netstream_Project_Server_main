using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 립모션과 Animation Rigging 이 동시에 작동하지 않는 문제가 있음
// 립모션용 캐릭터와 Animation Rigging 캐릭터를 따로 (2개) 만들어서 
// 이 파일에서 립모션 데이터를 읽어 Animation Rigging 캐릭터에게 넘겨줌
// 캐릭터마다 신체부위명이 다를 수 있어 Tag 명을 만들어 구별함 

public class FifthLeapMotionController : MonoBehaviour
{
    // 1. 립모션 캐릭터의 손 (트래킹 데이터를 읽어서)
    Transform leftLeapHand, rightLeapHand;


    // 2. 변수에 저장했다가
    private Dictionary<string, Vector3> leftPosDict; 
    private Dictionary<string, Quaternion> leftRotDict; 
    private Dictionary<string, Vector3> rightPosDict; 
    private Dictionary<string, Quaternion> rightRotDict; 

    Transform[] leftLeapAllChildren, rightLeapAllChildren;


    // 3. Animation Rigging 이 작동하는 캐릭터에 타겟(손끝)값을 전달함 
    // Animation Rigging 캐릭터의 손 
    Transform leftAnimHand, rightAnimHand;
    // 립모션 캐릭터의 손 데이터 => Animation Rigging 캐릭터 (위치만 전달하고, 립모션캐릭터는 숨겨서 보이지 않게함)

    // Animation Rigging 할때 타겟(손끝)이 있는데, 그 타겟의 Transform 값     
    Transform leftTargetThumb, leftTargetIndex, leftTargetMiddle, leftTargetRing, leftTargetPinky;
    Transform rightTargetThumb, rightTargetIndex, rightTargetMiddle, rightTargetRing, rightTargetPinky;

    // Animation 캐릭터에서 타겟 부위의 name 값을 받으려고만든 태그변수 (캐릭터가 바뀌어도 코드수정하지 않아도 되게)
    Transform leftAnimThumb, leftAnimIndex, leftAnimMiddle, leftAnimRing, leftAnimPinky;
    Transform rightAnimThumb, rightAnimIndex, rightAnimMiddle, rightAnimRing, rightAnimPinky;

    // Start is called before the first frame update
    void Start()
    {
        leftPosDict = new Dictionary<string, Vector3>(); 
        rightPosDict = new Dictionary<string, Vector3>(); 
        leftRotDict = new Dictionary<string, Quaternion>(); 
        rightRotDict = new Dictionary<string, Quaternion>();

        // 태그를 설정해서 객체찾을 수 있게함 
        leftLeapHand = GameObject.FindWithTag("LeapMotionL").transform; // 립모션 왼손
        rightLeapHand = GameObject.FindWithTag("LeapMotionR").transform; // 오른손

        leftAnimHand = GameObject.FindWithTag("AnimationL").transform; // 리깅 ArmRig 아래 Target 
        rightAnimHand = GameObject.FindWithTag("AnimationR").transform; 

        leftTargetThumb = GameObject.FindWithTag("TargetThumbL").transform; // 리깅 FingerRig 아래 Target 들
        leftTargetIndex = GameObject.FindWithTag("TargetIndexL").transform;
        leftTargetMiddle = GameObject.FindWithTag("TargetMiddleL").transform;
        leftTargetRing = GameObject.FindWithTag("TargetRingL").transform;
        leftTargetPinky = GameObject.FindWithTag("TargetPinkyL").transform;

        rightTargetThumb = GameObject.FindWithTag("TargetThumbR").transform;
        rightTargetIndex = GameObject.FindWithTag("TargetIndexR").transform;
        rightTargetMiddle = GameObject.FindWithTag("TargetMiddleR").transform;
        rightTargetRing = GameObject.FindWithTag("TargetRingR").transform;
        rightTargetPinky = GameObject.FindWithTag("TargetPinkyR").transform;

        leftAnimThumb = GameObject.FindWithTag("AnimationThumbL").transform; // 리깅 캐릭터 손가락
        leftAnimIndex = GameObject.FindWithTag("AnimationIndexL").transform;
        leftAnimMiddle = GameObject.FindWithTag("AniimationMiddleL").transform;
        leftAnimRing = GameObject.FindWithTag("AnimationRingL").transform;
        leftAnimPinky = GameObject.FindWithTag("AnimationPinkyL").transform;

        rightAnimThumb = GameObject.FindWithTag("AnimationThumbR").transform;
        rightAnimIndex = GameObject.FindWithTag("AnimationIndexR").transform;
        rightAnimMiddle = GameObject.FindWithTag("AniimationMiddleR").transform;
        rightAnimRing = GameObject.FindWithTag("AnimationRingR").transform;
        rightAnimPinky = GameObject.FindWithTag("AnimationPinkyR").transform;


        leftLeapAllChildren = leftLeapHand.gameObject.GetComponentsInChildren<Transform>();
        rightLeapAllChildren = rightLeapHand.gameObject.GetComponentsInChildren<Transform>();

        // 변수 준비 
        foreach(var child in leftLeapAllChildren){
            leftPosDict.Add(child.name, child.position);
            leftRotDict.Add(child.name, child.rotation);
        }
        foreach(var child in rightLeapAllChildren){
            rightPosDict.Add(child.name, child.position);
            rightRotDict.Add(child.name, child.rotation);
        }
        StartCoroutine(Tracking());
        
    }
    IEnumerator Tracking() { // 립모션데이터 받아서 변수에 저장하기 

        while(true) {
            // (1) 손 위치 맞춰두기 (리깅캐릭터, 립모션캐릭터 간)
            leftAnimHand.position = leftLeapHand.position;
            leftAnimHand.rotation = leftLeapHand.rotation;
            rightAnimHand.position = rightLeapHand.position;
            rightAnimHand.rotation = rightLeapHand.rotation;

            // (2) 손 객체 하위객체 순회하면서 트래킹좌표를 변수에 저장
            foreach (var child in leftLeapAllChildren){ // 왼손
                leftPosDict[child.name] = child.position;
                leftRotDict[child.name] = child.rotation;
            }
            foreach (var child in rightLeapAllChildren){ // 오른손 
                rightPosDict[child.name] = child.position;
                rightRotDict[child.name] = child.rotation;
            }
            // 양 손의 위치들이 한 프레임 모이면, 리깅캐릭터에 동기화시킴
            yield return StartCoroutine(SyncFingers());
        }
    }

    
    IEnumerator SyncFingers() { // 립모션에서 측정한 데이터 중 필요한 데이터를 리깅캐릭터에 전달

        leftTargetThumb.position = leftPosDict[leftAnimThumb.name];
        leftTargetIndex.position = leftPosDict[leftAnimIndex.name];
        leftTargetMiddle.position = leftPosDict[leftAnimMiddle.name];
        leftTargetRing.position = leftPosDict[leftAnimRing.name];
        leftTargetPinky.position = leftPosDict[leftAnimPinky.name];

        leftTargetThumb.rotation = leftRotDict[leftAnimThumb.name];
        leftTargetIndex.rotation = leftRotDict[leftAnimIndex.name];
        leftTargetMiddle.rotation = leftRotDict[leftAnimMiddle.name];
        leftTargetRing.rotation = leftRotDict[leftAnimRing.name];
        leftTargetPinky.rotation = leftRotDict[leftAnimPinky.name];

        rightTargetThumb.rotation = rightRotDict[rightAnimThumb.name];
        rightTargetIndex.rotation = rightRotDict[rightAnimIndex.name];
        rightTargetMiddle.rotation = rightRotDict[rightAnimMiddle.name];
        rightTargetRing.rotation = rightRotDict[rightAnimRing.name];
        rightTargetPinky.rotation = rightRotDict[rightAnimPinky.name];

        rightTargetThumb.position = rightPosDict[rightAnimThumb.name];
        rightTargetIndex.position = rightPosDict[rightAnimIndex.name];
        rightTargetMiddle.position = rightPosDict[rightAnimMiddle.name];
        rightTargetRing.position = rightPosDict[rightAnimRing.name];
        rightTargetPinky.position = rightPosDict[rightAnimPinky.name];


        yield return null;
    }
    private void OnDisable()
    {
        StopCoroutine(Tracking());
        StopCoroutine(SyncFingers());
    }

}
