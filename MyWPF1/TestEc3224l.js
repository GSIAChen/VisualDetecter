var ec3224lControl = new Ec3224lControl("2");
var mtsControl = new MtsControl(ec3224lControl);
var mtsRunControl = new MtsRunControl(engine, ec3224lControl, 0, 0);
projectAgent.connectMsg( mtsRunControl );
projectAgent.connectMsg( mtsControl );
var cameraFactory = new DahFactory();
initCamera(cameraFactory);

var imagePipo = new ImagePipo(8001, "127.0.0.1");
projectAgent.connectMsg(imagePipo);
commandAgent.connectSlot(mtsRunControl, "captureImageExt(int, int, const QImage &)", imagePipo, "sendImage(int, int, const QImage &)");
commandAgent.connectSlot(imagePipo, "receieveResult(int, int)", mtsRunControl, "receieveResult(int, int)");
commandAgent.connectSlot(imagePipo, "sendDeviceStart()", mtsRunControl, "start()");
commandAgent.connectSlot(imagePipo, "sendDeviceStop()",  mtsRunControl, "stop()");

function initCamera(factory)
{
   // 注释下面两行为demo
   factory.enumDevices();
   // 相机注意使用下降沿触发
   factory.generateCamera(0);
   factory.generateCamera(1);
   factory.generateCamera(2);
   factory.generateCamera(3);
   factory.generateCamera(4);
   factory.generateCamera(5);
}

function initDatabase(factory)
{
    var dbCfg = new Object;
    dbCfg.driverName = "QSQLITE";
    dbCfg.database = ":memory:";
    factory.appendDevice("in_mem_db", dbCfg, true);
}

function afterWidgetOpen()
{
    projectAgent.addMenu("相机");
    projectAgent.addMenu("筛选机");
    projectAgent.addMenu("通讯");
    projectAgent.connectMsg(ec3224lControl);
    camera1 = cameraFactory.getDeviceByName("Camera1");
    //camera1.triggerType=0;
    camera2 = cameraFactory.getDeviceByName("Camera2");
    //camera2.triggerType=0;
    camera3 = cameraFactory.getDeviceByName("Camera3");
    //camera3.triggerType=0;
    camera4 = cameraFactory.getDeviceByName("Camera4");
    //camera4.triggerType=0;
    camera5 = cameraFactory.getDeviceByName("Camera5");
    //camera5.triggerType=0;
    camera6 = cameraFactory.getDeviceByName("Camera6");
    //camera6.triggerType=0;
    projectAgent.addActionFunc("相机", "Camera1", "camera1.generalControlWidget().show()");
    projectAgent.addActionFunc("相机", "Camera2", "camera2.generalControlWidget().show()");
    projectAgent.addActionFunc("相机", "Camera3", "camera3.generalControlWidget().show()");
    projectAgent.addActionFunc("相机", "Camera4", "camera4.generalControlWidget().show()");
    projectAgent.addActionFunc("相机", "Camera5", "camera5.generalControlWidget().show()");
    projectAgent.addActionFunc("相机", "Camera6", "camera6.generalControlWidget().show()");
    projectAgent.addActionFunc("筛选机", "底层硬件控制", "ec3224lControl.generalControlWidget().show();");
    projectAgent.addActionFunc("筛选机", "相机位置测算控制", "mtsControl.generalControlWidget().show();");
    projectAgent.addActionFunc("筛选机", "筛选控制", "mtsRunControl.generalControlWidget().show();");
    projectAgent.setCentralWidget(mtsRunControl.generalControlWidget());

    mtsControl.setDevice(ec3224lControl, 0, 0);
    mtsControl.appendCamera(camera1, 0); 
    mtsControl.appendCamera(camera2, 1);
    mtsControl.appendCamera(camera3, 2);
    mtsControl.appendCamera(camera4, 3);
    mtsControl.appendCamera(camera5, 4);
    mtsControl.appendCamera(camera6, 5);

    configDevice();

    imagePipo.connectToHost();
    projectAgent.addActionFunc("通讯", "tcpSocket客户端", "imagePipo.generalControlWidget().show();");
}

function configDevice()
{
    var cameraCfg = new Object;
    cameraCfg.name = "camera1";
    cameraCfg.actionIO = 0;
    cameraCfg.position = 3120;
    cameraCfg.dir = 0;
    cameraCfg.mode = 5;
    mtsRunControl.addCamera(cameraCfg);
    cameraCfg.name = "camera2";
    cameraCfg.actionIO = 1;
    cameraCfg.position = 7265;
    mtsRunControl.addCamera(cameraCfg);
    cameraCfg.name = "camera3";
    cameraCfg.actionIO = 2;
    cameraCfg.position = 10255;
    cameraCfg.mode = 5;
    mtsRunControl.addCamera(cameraCfg);
    cameraCfg.name = "camera4";
    cameraCfg.actionIO = 3;
    cameraCfg.position = 16280;
    mtsRunControl.addCamera(cameraCfg);
    cameraCfg.name = "camera5";
    cameraCfg.actionIO = 4;
    cameraCfg.position = 20535;
    mtsRunControl.addCamera(cameraCfg);
    cameraCfg.name = "camera6";
    cameraCfg.actionIO = 5;
    cameraCfg.position = 24595;
    mtsRunControl.addCamera(cameraCfg);
    var blowCfg = new Object;
    blowCfg.actionIO = 15;
    blowCfg.position = 41651;
    blowCfg.dir = 0;
    blowCfg.mode = 5;
    mtsRunControl.addBlow(blowCfg);
    blowCfg.actionIO = 14;
    blowCfg.position = 39282;
    mtsRunControl.addBlow(blowCfg);
}
//var dbFactory = new DataBaseFactory();
//initDatabase(dbFactory);

// ec3224lControl.getOpenNum();
// ec3224lControl.getAllOutput(0);
// ec3224lControl.getAllInput(0);
// ec3224lControl.getServoEnable(0, 0);
// ec3224lControl.setServoEnable(0, false, 0);
//ec3224lControl.generalControlWidget().show();
// ec3224lControl.setEmgTriggerBit(0, 1, 0);
// ec3224lControl.setPosition(0,0,0);
// mtsControl.generalControlWidget().show();
// mtsRunControl.generalControlWidget().show();
mtsRunControl.setDeviceProperty("运行速度", 8000);
mtsRunControl.setCaptureTwice(false);
ec3224lControl.setServoEnable(0, true, 0)
ec3224lControl.setPulseMode(0,1,0);
