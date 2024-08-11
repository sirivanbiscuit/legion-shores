using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour
{
    public new Camera camera;

    public const int MIN_ZOOM = 4, MAX_ZOOM = 24;

    private float boundPushX, boundPushY;

    private float minPosX, maxPosX;
    private float minPosY, maxPosY;

    private float ratio;

    private Vector3 origin;

    private bool init = false;
    private int mapSize = 256; // starts at min size

    public void BindTo(int mapSize)
    {
        this.mapSize = mapSize;
        init = true;
    }

    void Start()
    {
        ratio = camera.pixelWidth / (float)camera.pixelHeight;
        ResetBounds();
        ReaffirmBounds();
    }

    void Update()
    {
        if (!init) return;

        if (Input.GetMouseButtonDown(2))
            origin = camera.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(2))
        {
            Vector3 to = camera.transform.position + origin -
                camera.ScreenToWorldPoint(Input.mousePosition);

            if (to.x >= minPosX && to.x <= maxPosX) SetX(to.x);
            if (to.y >= minPosY && to.y <= maxPosY) SetY(to.y);
        }

        float result = camera.orthographicSize
            + Input.mouseScrollDelta.y * -2;
        if (result != camera.orthographicSize &&
            result >= MIN_ZOOM && result <= MAX_ZOOM)
        {
            camera.orthographicSize = result;
            ResetBounds();
            ReaffirmBounds();
        }
    }

    void ResetBounds()
    {
        float s = camera.orthographicSize;
        float xB = 2 * (mapSize / 4) - (s * ratio);
        minPosX = -xB + (boundPushX < 0 ? boundPushX * s : 0);
        maxPosX = xB + (boundPushX > 0 ? boundPushX * s : 0);
        minPosY = s + (boundPushY < 0 ? boundPushY * s : 0);
        maxPosY = 2 * (mapSize / 4) - s + (boundPushY > 0 ? boundPushY * s : 0);
    }

    void ReaffirmBounds()
    {
        Vector3 get = camera.transform.position;
        if (get.x > maxPosX) SetX(maxPosX);
        else if (get.x < minPosX) SetX(minPosX);
        if (get.y > maxPosY) SetY(maxPosY);
        else if (get.y < minPosY) SetY(minPosY);
    }

    void SetX(float x)
    => camera.transform.position = new(
            x, camera.transform.position.y, camera.transform.position.z);

    void SetY(float y)
    => camera.transform.position = new(
            camera.transform.position.x, y, camera.transform.position.z);

}
