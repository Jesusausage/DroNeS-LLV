using System;
    private readonly ICameraMovement move;

    #region Properties
    public float MoveSpeed 
    }
        get { return _zoomSpeed; }
    }
    {
        get { return _rotationSpeed; }
    }
    {
    {
    {
    {
    {
        get { return _upperPitch; }

        set
        {
            _upperPitch = KeepAcute(value);
    }
    #endregion

    #region Methods
    {
        if (angle < 0) { return 0; }
        if (angle > 90) { return 90; }
        return angle;
    }
    {
        return (number < 0) ? 0 : number;
    }
    public void MoveLongitudinal(float input)

    #endregion