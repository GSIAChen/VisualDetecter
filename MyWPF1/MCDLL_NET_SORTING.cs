using System.Runtime.InteropServices;
/********************************************************************************************************************************************************************                                                   
1 ÿ��������Ե������ô�������,��Ӧ�����಻ͬ���
2 ���״̬ʵʱ���Դ洢10������,��ֹ�������ʵʱ
3 �����������´ﶯ���������û�ʹ��
4 �����ڸ��ֹ�������ֹOK��NGռ��ͬһ��ͨ��													  
********************************************************************************************************************************************************************/
namespace MCDLL_NET
{
    public class CMCDLL_NET_Sorting
    {
        /********************************************************************************************************************************************************************
                                                              1 ϵͳ���ú���  
        ********************************************************************************************************************************************************************/
        //1.0 ɸѡ���ܳ�ʼ������ ������ MCF_Open_Net() ǰ����
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Init_Net", SetLastError = false)]
        public static extern short MCF_Sorting_Init_Net(ushort StationNumber = 0);
        //1.1 ���ƿ��򿪹رպ���                              [1,100]                          [0,99]                          �궨��1.1                     
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Open_Net")]
        public static extern short MCF_Open_Net(ushort Connection_Number, ref ushort Station_Number, ref ushort Station_Type);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Open_Net")]
        public static extern short MCF_Get_Open_Net(ref ushort Connection_Number, ref ushort Station_Number, ref ushort Station_Type);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Close_Net")]
        public static extern short MCF_Close_Net();
        //1.2 ���ӳ�ʱ����ֹͣ����                             [0,60000]
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Link_TimeOut_Net")]
        public static extern short MCF_Set_Link_TimeOut_Net(uint Time_1MS, uint TimeOut_Output, ushort StationNumber = 0);
		//    ���ӳ�ʱ����ֹͣ����ʹ�ܺ���             
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Trigger_Output_Bit_Net")]
        public static extern short MCF_Set_Trigger_Output_Bit_Net(ushort Bit_Output_Number, ushort Bit_Output_Enable, ushort StationNumber = 0);
        
        //1.3 ���Ӽ�⺯��
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Link_State_Net")]
        public static extern short MCF_Get_Link_State_Net(ushort StationNumber = 0);

        /********************************************************************************************************************************************************************
                                                              2 ͨ�������������
        ********************************************************************************************************************************************************************/
        //2.1 ͨ��IOȫ���������                               [OUT31,OUT0]                     [0,99]   [10000,10099]               
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Output_Net")]
        public static extern short MCF_Set_Output_Net(uint All_Output_Logic, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Output_Net")]
        public static extern short MCF_Get_Output_Net(ref uint All_Output_Logic, ushort StationNumber = 0);
        //2.2 ͨ��IO��λ�������                               �궨��2.3.1                      �궨��2.3.2                      [0,99]  [10000,10099]   
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Output_Bit_Net")]
        public static extern short MCF_Set_Output_Bit_Net(ushort Bit_Output_Number, ushort Bit_Output_Logic, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Output_Bit_Net")]
        public static extern short MCF_Get_Output_Bit_Net(ushort Bit_Output_Number, ref ushort Bit_Output_Logic, ushort StationNumber = 0);
		//2.4 ͨ��IOȫ�����뺯��                               [Input31,Input0]                 [Input48,Input32]               [0,99]  [10000,10099]  
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Input_Net")]
        public static extern short MCF_Get_Input_Net(ref  uint All_Input_Logic1, ref  uint All_Input_Logic2, ushort StationNumber = 0);
        //2.5 ͨ��IO��λ���뺯��                               �궨��2.4.1                      �궨��2.4.2                     [0,99] 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Input_Bit_Net")]
        public static extern short MCF_Get_Input_Bit_Net(ushort Bit_Input_Number, ref ushort Bit_Input_Logic, ushort StationNumber = 0);

        /********************************************************************************************************************************************************************
                                                              3 ��ר�������������
        ********************************************************************************************************************************************************************/
        //3.1 �ŷ�ʹ�����ú���                              						 �궨��0.0    �궨��3.1          [0,99] 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Servo_Enable_Net")]
        public static extern short MCF_Set_Servo_Enable_Net(ushort Axis, ushort Servo_Logic, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Servo_Enable_Net")]
        public static extern short MCF_Get_Servo_Enable_Net(ushort Axis, ref ushort Servo_Logic, ushort StationNumber = 0);
        //3.2 �ŷ�������λ���ú���                         							 �궨��0.0    �궨��3.2           [0,99] 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Servo_Alarm_Reset_Net")]
        public static extern short MCF_Set_Servo_Alarm_Reset_Net(ushort Axis, ushort Alarm_Logic, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Servo_Alarm_Reset_Net")]
        public static extern short MCF_Get_Servo_Alarm_Reset_Net(ushort Axis, ref ushort Alarm_Logic, ushort StationNumber = 0);
        //3.3 �ŷ����������ȡ����                         							 �궨��0.0    �궨��3.3                    [0,99] 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Servo_Alarm_Net")]
        public static extern short MCF_Get_Servo_Alarm_Net(ushort Axis, ref ushort Servo_Alarm_State, ushort StationNumber = 0);
        /********************************************************************************************************************************************************************
                                                              4 �����ú���
        ********************************************************************************************************************************************************************/
        //4.1 ����ͨ��������ú���                        							 �궨��0.0    �궨��4.1  [0,99] 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Pulse_Mode_Net")]
        public static extern short MCF_Set_Pulse_Mode_Net(ushort Axis, uint Pulse_Mode, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Pulse_Mode_Net")]
        public static extern short MCF_Get_Pulse_Mode_Net(ushort Axis, ref uint Pulse_Mode, ushort StationNumber = 0);
        //4.2 λ�����ú��� 															 �궨��0.0    [-2^31,(2^31-1)]    [0,99] 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Position_Net")]
        public static extern short MCF_Set_Position_Net(ushort Axis, int Position, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Position_Net")]
        public static extern short MCF_Get_Position_Net(ushort Axis, ref int Position, ushort StationNumber = 0);
        //4.3 ���������ú���                          								 �궨��0.0    [-2^31,(2^31-1)]  [0,99] 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Encoder_Net")]
        public static extern short MCF_Set_Encoder_Net(ushort Axis, int Encoder, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Encoder_Net")]
        public static extern short MCF_Get_Encoder_Net(ushort Axis, ref int Encoder, ushort StationNumber = 0);
        //4.4 �ٶȻ�ȡ                            									 �궨��0.0    [-2^15,(2^15-1)]      [-2^15,(2^15-1)]        [0,99] 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Vel_Net")]
        public static extern short MCF_Get_Vel_Net(ushort Axis, ref double Command_Vel, ref double Encode_Vel, ushort StationNumber = 0);
        /********************************************************************************************************************************************************************
                                                              5 ��Ӳ������ֹͣ�˶�����
        ********************************************************************************************************************************************************************/
        //5.1 ͨ��IO���븴�ã���Ϊ����ֹͣ����                 					�궨��2.4.1              �궨��5.1      [0,99] 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_EMG_Bit_Net")]
        public static extern short MCF_Set_EMG_Bit_Net(ushort EMG_Input_Number, ushort EMG_Mode, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_EMG_Output_Net")]
        public static extern short MCF_Set_EMG_Output_Net(ushort EMG_Input_Number, ushort EMG_Mode, uint EMG_Output, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_EMG_Output_Enable_Net")]
        public static extern short MCF_Set_EMG_Output_Enable_Net(ushort Bit_Output_Number, ushort Bit_Output_Enable, ushort StationNumber = 0);
        //5.11 ��״̬����ֹͣ�˶���ѯ����                             				�궨��0.0           MC_Retrun.h[0,28]      [0,99]  
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Axis_State_Net")]
        public static extern short MCF_Get_Axis_State_Net(ushort Axis, ref short Reason, ushort StationNumber = 0);
        /********************************************************************************************************************************************************************
                                                              7 ��λ�˶����ƺ���
        ********************************************************************************************************************************************************************/
        //7.1 �ٶȿ��ƺ���                     										 �궨��0.0    (0,10M]P/S   (0,1T]P^2/S    [0,99]
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_JOG_Net")]
        public static extern short MCF_JOG_Net(ushort Axis, double dMaxV, double dMaxA, ushort StationNumber = 0);
        //7.4 �������ߺ���                                  						 �궨��0.0    [0,dMaxV]      (0,10M]P/S   (0,1T]P^2/S   (0,100T]P^3/S [0,dMaxV]       �궨��0.4       [0,99] 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Axis_Profile_Net")]
        public static extern short MCF_Set_Axis_Profile_Net(ushort Axis, double dV_ini, double dMaxV, double dMaxA, double dJerk, double dV_end, ushort Profile, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Axis_Profile_Net")]
        public static extern short MCF_Get_Axis_Profile_Net(ushort Axis, ref double dV_ini, ref double dMaxV, ref double dMaxA, ref double dJerk, ref double dV_end, ref ushort Profile, ushort StationNumber = 0);
        //7.5 �����˶�����                          								 �궨��0.0   [-2^31,(2^31-1)]  �궨��0.3       [0,99] 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Uniaxial_Net")]
        public static extern short MCF_Uniaxial_Net(ushort Axis, int dDist, ushort Position_Mode, ushort StationNumber = 0);
        //7.6 ����ֹͣ���ߺ���                                  					 	�궨��0.0     (0,1T]P^2/S  (0,100T]P^3/S  �궨��0.4       [0,99]   
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Axis_Stop_Profile_Net")]
        public static extern short MCF_Set_Axis_Stop_Profile_Net(ushort Axis, double dMaxA, double dJerk, ushort Profile, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Axis_Stop_Profile_Net")]
        public static extern short MCF_Get_Axis_Stop_Profile_Net(ushort Axis, ref double dMaxA, ref double dJerk, ref ushort Profile, ushort StationNumber = 0);
        //7.7 ��ֹͣ����                             								 �궨��0.0    �궨��7.7              [0,99] 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Axis_Stop_Net")]
        public static extern short MCF_Axis_Stop_Net(ushort Axis, ushort Axis_Stop_Mode, ushort StationNumber = 0);

        /********************************************************************************************************************************************************************
                                                               16 ϵͳ����
        ********************************************************************************************************************************************************************/
        //16.1 ģ��汾��                              								[0x00000000,0xFFFFFFFF] [0,99]  
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Version_Net")]
        public static extern short MCF_Get_Version_Net(ref uint Version, ushort StationNumber = 0);
        //16.2 ���к�                                         						[0x00000000,0xFFFFFFFF] [0,99]  
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Serial_Number_Net")]
        public static extern short MCF_Get_Serial_Number_Net(ref long Serial_Number, ushort StationNumber = 0);
        //16.3 ģ������ʱ��                                        					[0x00000000,0xFFFFFFFF] [0,99]    ��λ����  
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Run_Time_Net")]
        public static extern short MCF_Get_Run_Time_Net(ref uint Run_Time, ushort StationNumber = 0);
        //16.4 Flash ��д����Ŀǰ��ʱ��С2Kbytes,Ҳ������һ�� unsigned int Array[256] �������
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Flash_Write_Net")]
        public static extern short MCF_Flash_Write_Net(uint Pass_Word_Setup, ref uint Flash_Write_Data, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Flash_Read_Net")]
        public static extern short MCF_Flash_Read_Net(uint Pass_Word_Check, ref uint Flash_Read_Data, ushort StationNumber = 0);
        //16.8 ϵͳ��ʱ�ص�����
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_CallBack_Net")]
        public static extern short MCF_Set_CallBack_Net(int CallBack, uint Time_1MS);



        /********************************************************************************************************************************************************************
                                                              101 �ر��Զ�ɸѡ���ܲ��������,���,��������      ע�⣺���øú�����ſ�������102,103,104��Ŀ����    
        ********************************************************************************************************************************************************************/
        //101.1 ���ò���ǰ��������ȹر�ɸѡ����
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Close_Net")]
        public static extern short MCF_Sorting_Close_Net(ushort StationNumber = 0);
        /********************************************************************************************************************************************************************
                                                              102 �������ϼ�⹦��,�û�������Ҫ����             ע�⣺�Զ�ɸѡʱ��ֹ����
        ********************************************************************************************************************************************************************/
        //102.1 ��������С�ߴ�
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Piece_Size_Net")]
        public static extern short MCF_Sorting_Set_Piece_Size_Net(uint Max_Size, uint Min_Size, ushort StationNumber = 0);
        //102.2 �����ȫ����,��ȫʱ��
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Piece_Place_Net")]
        public static extern short MCF_Sorting_Set_Piece_Place_Net(uint Min_Distance, uint Min_Time_Intervel, ushort StationNumber = 0);
		//102.3 ���ϼ������
 		//      ���ϼ��ʹ��(Ĭ��Bit_Output_0,Bit_Output_1��,0���� 1����) [Bit_Input_0,Bit_Input_3]
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Input_Enable_Net")]
        public static extern short MCF_Sorting_Set_Input_Enable_Net(ushort Bit_Input_Number, ushort Bit_Input_Enable, ushort StationNumber = 0);
		//      ���ϼ���ƽ(Ĭ��ȫ���͵�ƽ��  0���͵�ƽ  1���ߵ�ƽ)      [Bit_Input_0,Bit_Input_3]
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Input_Logic_Net")]
        public static extern short MCF_Sorting_Set_Input_Logic_Net(ushort Bit_Input_Number, ushort Bit_Input_Logic, ushort StationNumber = 0);
		//      ���ϼ�������(Ĭ��ȫ������Axis_1������)                  [Bit_Input_0,Bit_Input_3]
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Input_Source_Net")]
        public static extern short MCF_Sorting_Set_Input_Source_Net(ushort Bit_Input_Number, ushort Axis, ushort Source, ushort StationNumber = 0);
		//      ���ϼ�Ⲷ��λ��(Ĭ��ȫ������ǰ����  0��ǰ��  1���м�)    [Bit_Input_0,Bit_Input_3]
		[DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Input_Position_Net")]
        public static extern short MCF_Sorting_Set_Input_Position_Net(ushort Bit_Input_Number, ushort Mode, ushort StationNumber = 0);
//      ���ϼ���Զ����������(������Զ��ر�ʹ��)                 [Bit_Input_0,Bit_Input_3]
		[DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Input_Clear_Encoder_Net")]
        public static extern short MCF_Sorting_Set_Input_Clear_Encoder_Net(ushort Bit_Input_Number,ushort Enable,ushort StationNumber = 0);
		//102.4 DI00 ��ⲻ�������ǿ�Ʊ�������
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Piece_Keep_Net")]
        public static extern short MCF_Sorting_Set_Piece_Keep_Net(uint Keep_Length, ushort StationNumber = 0);
		//102.5 DI00 ���������ϳ�ʱֹͣ���˶�(Ĭ��ʱ��0,��ʾ������)      [0,60000]             &Array[Channel00,Channel15],0:�� 1:�� 2:�ر�  
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Input_0_TimeOut_Net")]
        public static extern short MCF_Sorting_Set_Input_0_TimeOut_Net(uint Time_1MS, ref uint TimeOut_Output, ushort StationNumber = 0);
		//                                                                 [0,15]                [DO00,DO47]         
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Input_0_Config_Net")]
        public static extern short MCF_Sorting_Set_Input_0_Config_Net(ushort Channel, ushort Bit_Input_Number, ushort StationNumber = 0);
        //102.6 ͨ��IO��λ�����˲�����                                      [Bit_Input_0,Bit_Input_1]        [1,100]MS                     [0,99] 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Input_Filter_Time_Bit_Net")]
        public static extern short MCF_Set_Input_Filter_Time_Bit_Net(ushort Bit_Input_Number, uint Filter_Time_1MS, ushort StationNumber = 0);
		//102.7 ������ϼ����������,OK����,NG����                    [Bit_Input_1]
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Input_Bind_Net")]
        public static extern short MCF_Sorting_Set_Input_Bind_Net(ushort Bit_Input_Number, ushort Camera_Start_Number, ushort Bond_Start_Number, ushort StationNumber = 0);
        /********************************************************************************************************************************************************************
                                                              103 ����OK,NG��ȫ��������,�û�������Ҫ����       ע�⣺�Զ�ɸѡʱ��ֹ����
        ********************************************************************************************************************************************************************/
		//103.1 �������OK��ʱֹͣ���˶�(Ĭ��ʱ��0,��ʾ������)              [0,60000]             &Array[DO00,DO15],0:�� 1:�� 2:�ر� 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Trig_Blow_OK_TimeOut_Net")]
        public static extern short MCF_Sorting_Set_Trig_Blow_OK_TimeOut_Net(uint Time_1MS, ref uint TimeOut_Output, ushort StationNumber = 0);
		//103.2 �����������NGֹͣ���˶�(Ĭ��ʱ��0,��ʾ������)              [0,60000]              &Array[DO00,DO15],0:�� 1:�� 2:�ر�  
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Trig_Blow_NG_NumberOut_Net")]
        public static extern short MCF_Sorting_Set_Trig_Blow_NG_NumberOut_Net(uint NG_Number, ref uint NumberOut_Output, ushort StationNumber = 0);
        //103.3 HMC3432S/HMC3412S ��������������¼��ȷ�����ܣ��Դ��ж��Ƿ���
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Blow_Check_Again_Net")]
        public static extern short MCF_Sorting_Set_Blow_Check_Again_Net(ushort Bit_Input_Number, ushort Bit_Input_Logic, int Input_Position, uint Piece_Size, 
																		ushort Blow_OK_Check, 
																		ushort Blow_NG_Check, 
																		ushort Blow_1_Check, 
																		ushort Blow_2_Check, 
																		ushort Blow_3_Check, 
																		ushort Blow_4_Check, 
																		ushort Blow_5_Check, 
																		ushort Blow_6_Check, 
																		ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Blow_Check_Lose_Number_Net")]
        public static extern short MCF_Sorting_Get_Blow_Check_Lose_Number_Net(ref uint Lose_Number, ushort StationNumber = 0);
        /********************************************************************************************************************************************************************
                                                              104 ���������������,�û���������                ע�⣺�Զ�ɸѡʱ��ֹ����
        ********************************************************************************************************************************************************************/
        //104.1 HMC3432S ������������ʹ�������                             [1,30]                        [1,30]
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Camera_Blow_Config_Net")]
        public static extern short MCF_Sorting_Camera_Blow_Config_Net(ushort Camera_Number, ushort Blow_Number, ushort StationNumber = 0);
        //104.2 �����������                                                                              ����װ�õ����λ��   ���������������з��� 
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Camera_Net")]
        public static extern short MCF_Sorting_Set_Camera_Net(ushort Camera_Number, int Camera_Position, ushort Motion_Dir, ushort Action_Mode, ushort Action_IO, ushort StationNumber = 0); 
        
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Camera_Light_Frequency_Net")]
        public static extern short MCF_Sorting_Set_Camera_Light_Frequency_Net(ushort Camera_Number, ushort Light_Number, ushort Frequency_Enable, ushort StationNumber = 0);
		//    ���ù�Դ��ǰ����ʱ��ƫ��                                                                  [100,1000] ��λ��us
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Camera_Light_Early_Net")]
        public static extern short MCF_Sorting_Set_Camera_Light_Early_Net(ushort Camera_Number, ushort Early_Position, ushort StationNumber = 0);
		//    ������������½��س�ʱֹͣ��          
		[DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Camera_TimeOut_Net")]
        public static extern short MCF_Sorting_Set_Camera_TimeOut_Net(ushort Camera_Number, ushort Bit_Input_Number, ushort Time_1MS, ushort StationNumber = 0);

		//104.3 ���ô���������պ�,��ʱ���ٺ����������1,һ������Ϊ ���ڿ��ƿ��Ӵ���������յ������ͼ������Ҫ��ʱ�� [0,655] ��λ��ms
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Trig_Camera_Delay_Count_Net")]
        public static extern short MCF_Sorting_Set_Trig_Camera_Delay_Count_Net(ushort Camera_Number, double Camera_Delay_Count_MS, ushort StationNumber = 0);
        //104.4 ����OK��������
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Blow_OK_Net")]
        public static extern short MCF_Sorting_Set_Blow_OK_Net(int Blow_OK_Position, ushort Motion_Dir, ushort Action_Mode, ushort Action_IO, ushort StationNumber = 0);
        //104.5 ����NG��������
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Blow_NG_Net")]
        public static extern short MCF_Sorting_Set_Blow_NG_Net(int Blow_NG_Position, ushort Motion_Dir, ushort Action_Mode, ushort Action_IO, ushort StationNumber = 0);
        //104.6 ���ô���1��30����                                          [1,30]                  
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Blow_Net")]
        public static extern short MCF_Sorting_Set_Blow_Net(ushort Blow_Number, int Blow_Position, ushort Motion_Dir, ushort Action_Mode, ushort Action_IO, ushort StationNumber = 0);
		//104.7 �����������,OK����,NG��������������λ��ƫ�ƣ�ƫ�ƴ�СΪ�����С�ı���                  [0,100]
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Camera_Trig_Offset_Net")]
        public static extern short MCF_Sorting_Set_Camera_Trig_Offset_Net(ushort Camera_Number, short Size_Ratio, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Blow_OK_Trig_Offset_Net")]
		public static extern short MCF_Sorting_Set_Blow_OK_Trig_Offset_Net(short Size_Ratio, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Blow_NG_Trig_Offset_Net")]
		public static extern short MCF_Sorting_Set_Blow_NG_Trig_Offset_Net(short Size_Ratio, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Blow_Trig_Offset_Net")]
		public static extern short MCF_Sorting_Set_Blow_Trig_Offset_Net(ushort Blow_Number, short Size_Ratio, ushort StationNumber = 0);
        /*******************************************************************************************************************************************************************
                                                              105 �Զ�ɸѡ������������                         ע�⣺���øú������ֹ����102,103,104��Ŀ����
        ********************************************************************************************************************************************************************/
        //105.1 ɸѡ��������,�����úò���������
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Start_Net")]
        public static extern short MCF_Sorting_Start_Net(ushort Mode = 0, ushort StationNumber = 0);
        /********************************************************************************************************************************************************************
                                                              106 ����ͼ��������
        ********************************************************************************************************************************************************************/
		//106.0 ����������������ʱ������ʱʱ��,����������ʱ������
   		[DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Camera_Handle_Time_Net")]
        public static extern short MCF_Sorting_Set_Camera_Handle_Time_Net(ushort Camera_Number, double Handle_Time_1MS, double Handle_TimeOut_1MS, uint Handle_TimeOut_Number, ushort StationNumber = 0);
        //106.1 ����ģʽ0���û��ۺ��������������ʹ���ָ�� 
		//      �û���ͼ����ص������е��øú���֪ͨͼ������
   		[DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Camera_Result_Data_Net")]
        public static extern short MCF_Sorting_Set_Camera_Result_Data_Net(ushort Camera_Number, uint Result_Data, ushort StationNumber = 0);
        //      �û������̼߳���������µ�ͼ����
   		[DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Camera_Result_Updata_Net")]
        public static extern short MCF_Sorting_Get_Camera_Result_Updata_Net(ushort Camera_Number, ref uint Piece_Number, ushort StationNumber = 0);
   		[DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Camera_Result_Buffer_Net")]
        public static extern short MCF_Sorting_Get_Camera_Result_Buffer_Net(ushort Camera_Number, uint Piece_Number, ref uint Result_Buffer, ushort StationNumber = 0);
   		[DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Camera_Handle_Time_Net")]
        public static extern short MCF_Sorting_Get_Camera_Handle_Time_Net(ushort Camera_Number, uint Piece_Number, ref uint Handle_Time, ushort StationNumber = 0);
		//      �û�����ͼ��������	
		[DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Trig_Blow_OK_Net")]
        public static extern short MCF_Sorting_Set_Trig_Blow_OK_Net(uint Piece_Number, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Trig_Blow_NG_Net")]
        public static extern short MCF_Sorting_Set_Trig_Blow_NG_Net(uint Piece_Number, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Trig_Blow_Net")]
        public static extern short MCF_Sorting_Set_Trig_Blow_Net(ushort Blow_Number,uint Piece_Number, ushort StationNumber = 0);
        //106.2 ����ģʽ1���û�ֱ�ӷ���ÿ�������������ƿ��Զ��ۺϽ������
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Camera_Result_OK_Net")]
        public static extern short MCF_Sorting_Set_Camera_Result_OK_Net(ushort Camera_Number, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Camera_Result_NG_Net")]
        public static extern short MCF_Sorting_Set_Camera_Result_NG_Net(ushort Camera_Number, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Blow_Result_OK_Net")]
        public static extern short MCF_Sorting_Get_Blow_Result_OK_Net(ref uint Result_OK_Number, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Blow_Result_NG_Net")]
        public static extern short MCF_Sorting_Get_Blow_Result_NG_Net(ref uint Result_NG_Number, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Blow_Result_Miss_Net")]
        public static extern short MCF_Sorting_Get_Blow_Result_Miss_Net(ref uint Result_Miss_Number, ushort StationNumber = 0);

        //106.3 ����ģʽ2���û�����Ҫ���������������ƿ�ͨ��IO�ۺϽ������,ȫ��ͨ��Ӳ��ʵ��,����ֱ�����PLC       (0,500]
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Camera_Result_Input_OK_Net")]
        public static extern short MCF_Sorting_Set_Camera_Result_Input_OK_Net(ushort Camera_Number, ushort Input_Number, ushort Logic, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Set_Camera_Result_Input_NG_Net")]
        public static extern short MCF_Sorting_Set_Camera_Result_Input_NG_Net(ushort Camera_Number, ushort Input_Number, ushort Logic, ushort StationNumber = 0);
        /********************************************************************************************************************************************************************
                                                              107 ���,���,����״̬��⺯��
        ********************************************************************************************************************************************************************/
        //107.0 ɸѡ��⺯��
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_State_Net")]
        public static extern short MCF_Sorting_Get_State_Net(ref ushort State, ushort StationNumber = 0);
        
        //107.1 ��ȡDI00������ϸ��������
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Piece_Pass_Net")]
        public static extern short MCF_Sorting_Get_Piece_Pass_Net(ushort Piece_Input_Number, ref uint Piece_Pass, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Piece_Pass_Size_Net")]
        public static extern short MCF_Sorting_Get_Piece_Pass_Size_Net(ushort Piece_Input_Number, ref uint Piece_Pass_Size, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Piece_Pass_Size_Max_Net")]
        public static extern short MCF_Sorting_Get_Piece_Pass_Size_Max_Net(ushort Piece_Input_Number, ref uint Piece_Pass_Size_Max, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Piece_Pass_Size_Min_Net")]
        public static extern short MCF_Sorting_Get_Piece_Pass_Size_Min_Net(ushort Piece_Input_Number, ref uint Piece_Pass_Size_Min, ushort StationNumber = 0);
		[DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Piece_Pass_Dist_Net")]
        public static extern short MCF_Sorting_Get_Piece_Pass_Dist_Net(ushort Piece_Input_Number, ref uint Piece_Pass_Dist, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Piece_Pass_Time_Net")]
        public static extern short MCF_Sorting_Get_Piece_Pass_Time_Net(ushort Piece_Input_Number, ref uint Piece_Pass_Time, ushort StationNumber = 0);                
        //107.2 ��ȡDI00�����������
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Piece_State_Net")]
        public static extern short MCF_Sorting_Get_Piece_State_Net(ushort Piece_Input_Number,		 //�������������˿ں� 
																	ref uint Piece_Find, 			 //���ƥ��ͳ������
																	ref uint Piece_Size, 			 //�����С��10��
																	ref uint Piece_Distance_To_next, //�����࣬10��
																	ref uint Piece_Cross_Camera, 	 //������������������	
																	ushort StationNumber = 0);
        //107.3 ��ȡ�жϿ��ƿ�����������ռ���,ͼ����������һ��Ҫ������ʱ��Ŀ��ƿ�������ռ���������Ҫһһ��Ӧ,������Ϊͼ���쳣����©�Ĵ���
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Trig_Camera_Count_Net")]
        public static extern short MCF_Sorting_Get_Trig_Camera_Count_Net(ushort Camera_Number, ref uint Trig_Camera_Count, ushort StationNumber = 0);
        //107.4 ��ȡOK,NG��������
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Trig_Blow_NG_Count_Net")]
        public static extern short MCF_Sorting_Get_Trig_Blow_NG_Count_Net(ref uint Trig_Blow_NG_Count, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Trig_Blow_OK_Count_Net")]
        public static extern short MCF_Sorting_Get_Trig_Blow_OK_Count_Net(ref uint Trig_Blow_OK_Count, ushort StationNumber = 0);
        //107.5 ��ȡOK,NG©��������
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Lose_Blow_NG_Count_Net")]
        public static extern short MCF_Sorting_Get_Lose_Blow_NG_Count_Net(ref uint Lose_Blow_NG_Count, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Lose_Blow_OK_Count_Net")]
        public static extern short MCF_Sorting_Get_Lose_Blow_OK_Count_Net(ref uint Lose_Blow_OK_Count, ushort StationNumber = 0);
        //107.6 ��ȡ������������
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Sorting_Get_Trig_Blow_Count_Net")]
        public static extern short MCF_Sorting_Get_Trig_Blow_Count_Net(ushort Blow_Number, ref uint Trig_Blow_Count, ushort StationNumber = 0);
		/********************************************************************************************************************************************************************
															  16 ��Դ����������
		********************************************************************************************************************************************************************/
		//16.1 ���ù�Դģʽ(1MS��������)                  �궨��15.1.1         0���ر� 1:24V���� 2:24VƵ�� 3:48V����  
		[DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Light_Mode_Net")]
        public static extern short MCF_Set_Light_Mode_Net(ushort Channel, ushort Light_Mode, ushort StationNumber = 0); 
		
		//16.2 ���õ�������(1MS��������)                     �궨��15.1.1           [0,15000] ��λ��MA      Over_Current/1000*0.1*11/3.3 * 4095  
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Light_Current_Net")]
        public static extern short MCF_Set_Light_Current_Net(ushort Channel, ushort Max_Current, ushort StationNumber = 0);
		[DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Light_Current_1_4_Net")]
        public static extern short MCF_Get_Light_Current_1_4_Net(ref ushort Current_1, ref ushort Current_2, ref ushort Current_3, ref ushort Current_4, ushort StationNumber = 0);
        [DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Get_Light_Current_5_8_Net")]
        public static extern short MCF_Get_Light_Current_5_8_Net(ref ushort Current_5, ref ushort Current_6, ref ushort Current_7, ref ushort Current_8, ushort StationNumber = 0);
		
		//16.3 ���ù�Դ���(1MS��������)                       �궨��15.1.1           ����:[0,255] Ƶ��[0,1000]
		[DllImport("MCDLL_NET.DLL", EntryPoint = "MCF_Set_Light_Output_Net")]
        public static extern short MCF_Set_Light_Output_Net(ushort Channel, ushort Light_Size, ushort StationNumber = 0); 
				
    }
}