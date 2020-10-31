using System;
using System.Collections.Generic;
//using System.Data.OleDb;
using System.Data.SqlClient;
using System.Text;

namespace RigActivityApp
{
    //=================================================================
    class RigActivity
    {
        //-------------------------------------------------------------
        private SurfaceData CurrentData,LastData;

        private List<ActivitiesStruct> Activities;
        
        private List<SReceptorsStruct> SReceptors,SDictionary;

        public String Activity;


        private static String ConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\Vadim\\source\\repos\\RigActivityApp\\SurfaceData.mdf;Integrated Security=True";
        private SqlConnection conn;

        //-------------------------------------------------------------
        public RigActivity()
        {
            Filler();
        }

        //-------------------------------------------------------------
        public RigActivity(String _WellUID, float _Depth, DateTime _Time, float _HookLoad, float _SPP, float _BitDepth, float _RPM, float _Block)
        {
            Filler();

            CurrentData.WellUid = _WellUID;
            CurrentData.Depth= _Depth;
            CurrentData.Time = _Time;
            CurrentData.HookLoad = _HookLoad;
            CurrentData.RPM = _RPM;
            CurrentData.SPP = _SPP;
            CurrentData.BitDepth = _BitDepth;
            CurrentData.Block = _Block;
            Aggregator();
            Calculator();
            Perceptron();
            Finalizator();
        }

        //-------------------------------------------------------------
        public void Filler() // creating of dinamic components and activity list filling
        {
            CurrentData = new SurfaceData();
            LastData = new SurfaceData();
            
            SReceptors = new List<SReceptorsStruct>();
            SDictionary = new List<SReceptorsStruct>();
            SReceptorsStruct TempSReceptor;

            String query = "SELECT * FROM SReceptors";
            using (conn = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                try
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            TempSReceptor.SName = reader.GetString(1);
                            TempSReceptor.ActivityID = reader.GetInt32(2);
                            TempSReceptor.SExist = true;
                            TempSReceptor.SValue= reader.GetInt32(3);
                            SDictionary.Add(TempSReceptor);
                            //Console.WriteLine("{0}\t{1}", reader.GetInt32(0), reader.GetString(1));
                        }
                    }
                    else
                    {
                        Console.WriteLine("No Activities");
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    conn.Close();
                }

            }

            Activities = new List<ActivitiesStruct>();
            ActivitiesStruct TempActivity = new ActivitiesStruct();

            query = "SELECT * FROM Activities";
            using (conn = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                try
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            TempActivity.ActivityID = reader.GetInt32(0);
                            TempActivity.ActivityName = reader.GetString(1);
                            TempActivity.ActivityValue = true;
                            TempActivity.ActivityWeight= reader.GetInt32(3);
                            Activities.Add(TempActivity);
                            //Console.WriteLine("{0}\t{1}", reader.GetInt32(0), reader.GetString(1));
                        }
                    }
                    else
                    {
                        Console.WriteLine("No Activities");
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    
                    conn.Close();
                }

            }
        }

        //-------------------------------------------------------------
        private void Aggregator()
        {
            // get last RecordUID from DB
            String query = "SELECT MAX(RecordUID) FROM SD";
            using (conn = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                try
                {
                    conn.Open();
                    Int64 RecordUIDLast = (Int64)cmd.ExecuteScalar();
                    LastData.RecordUid = RecordUIDLast; // ошибка - последняя запись могла быть не от этой скважины
                    CurrentData.RecordUid = RecordUIDLast + 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Activity = "Error";
                    conn.Close();
                }
            }
            conn.Close();
            // get last record with surface data from DB
            using (conn = new SqlConnection(ConnectionString))
            {
                query = "SELECT * FROM SD WHERE WellUID='"+ CurrentData.WellUid + "' AND RecordUID=(SELECT MAX(RecordUID) FROM SD WHERE WellUID='" + CurrentData.WellUid + "');";
                SqlCommand cmd = new SqlCommand(query, conn);
                try
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            LastData.RecordUid= reader.GetInt64(0);
                            LastData.WellUid = reader.GetString(1);
                            LastData.Depth = reader.GetDouble(2);
                            //LastData.Time = reader.GetDateTime(3); - error with Data-Time format
                            LastData.BitDepth = reader.GetDouble(4);
                            LastData.HookLoad = reader.GetDouble(5);
                            LastData.SPP = reader.GetDouble(6);
                            LastData.Block = reader.GetDouble(7);
                            LastData.RPM = reader.GetDouble(8);
                            LastData.Activity = reader.GetString(9);

                            //Console.WriteLine("{0}\t{1}", reader.GetInt32(0), reader.GetString(1));
                        }
                    }
                    else
                    {
                        Console.WriteLine("No Last Surface Data");
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    conn.Close();
                }

                // add new record with surface data to DB   
                cmd = new SqlCommand("INSERT INTO SD VALUES (@RecordUid, @WellUid, @Depth, @Time, @BD, @HL, @SPP, @Block, @RPM, @Activity)", conn);
                cmd.Parameters.AddWithValue("@RecordUid", CurrentData.RecordUid);
                cmd.Parameters.AddWithValue("@WellUid", CurrentData.WellUid);
                cmd.Parameters.AddWithValue("@Depth", CurrentData.Depth);
                cmd.Parameters.AddWithValue("@Time", CurrentData.Time);
                cmd.Parameters.AddWithValue("@BD", CurrentData.BitDepth);
                cmd.Parameters.AddWithValue("@HL", CurrentData.HookLoad);
                cmd.Parameters.AddWithValue("@SPP", CurrentData.SPP);
                cmd.Parameters.AddWithValue("@Block", CurrentData.Block);
                cmd.Parameters.AddWithValue("@RPM", CurrentData.RPM);
                cmd.Parameters.AddWithValue("@Activity", "");
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Activity = "Error";
                    conn.Close();
                }

            }

        }

        //-------------------------------------------------------------
        private void Aggregator(String _WellUID, float _Depth, DateTime _Time, float _HookLoad, float _SPP, float _BitDepth, float _RPM, float _Block)
        {
            CurrentData.WellUid = _WellUID;
            CurrentData.Depth = _Depth;
            CurrentData.Time = _Time;
            CurrentData.HookLoad = _HookLoad;
            CurrentData.RPM = _RPM;
            CurrentData.SPP = _SPP;
            CurrentData.BitDepth = _BitDepth;
            CurrentData.Block = _Block;
            Aggregator();
        }

        //-------------------------------------------------------------
        private void Calculator() // math calculations and logic variables (S-receptors) creation
        {
            // rewrite for data from Sdictionary
            SReceptorsStruct sReceptor;
            //OnBottom
            sReceptor.SName = "OnBottom";
            sReceptor.SExist = true;
            sReceptor.ActivityID = -1;
            if (Math.Abs(CurrentData.Depth - CurrentData.BitDepth) < 0.1) //d=0.1m
            {
                sReceptor.SValue = 1;
            }
            else
            {
                sReceptor.SValue = 0;
            }
            SReceptors.Add(sReceptor);

            //OnSurface
            sReceptor.SName = "OnSurface";
            sReceptor.SExist = true;
            if (Math.Abs(CurrentData.BitDepth) < 0.1) //d=0.1m
            {
                sReceptor.SValue = 1;
            }
            else
            {
                sReceptor.SValue = 0;
            }
            SReceptors.Add(sReceptor);

            //Rotation
            sReceptor.SName = "Rotation";
            sReceptor.SExist = true;
            if (CurrentData.RPM >0) //d=0.00m
            {
                sReceptor.SValue = 1;
            }
            else
            {
                sReceptor.SValue = 0;
            }
            SReceptors.Add(sReceptor);

            //DepthInc
            sReceptor.SName = "DepthInc";
            sReceptor.SExist = true;
            if (CurrentData.Depth>LastData.Depth) //d=0.00m
            {
                sReceptor.SValue = 1;
            }
            else
            {
                sReceptor.SValue = 0;
            }
            SReceptors.Add(sReceptor);

            //BitTrip
            sReceptor.SName = "BitTrip";
            sReceptor.SExist = true;
            if (CurrentData.BitDepth - LastData.BitDepth > 0.01) //d=0.01m
            {
                sReceptor.SValue = 1;
            }
            else if(CurrentData.BitDepth - LastData.BitDepth < -0.01)
            {
                sReceptor.SValue = -1;
            }
            else
            {
                sReceptor.SValue = 0;
            }
            SReceptors.Add(sReceptor);

        }

        //-------------------------------------------------------------
        private void Perceptron()
        {
            ActivitiesStruct TempActivity = new ActivitiesStruct();
            bool ActivityValue=true;
            for (int i=0;i< Activities.Count;i++) // check logic for each activity
            {
                foreach (SReceptorsStruct ModelReceptor in SDictionary) // go though Dictionary and check needed for current activity
                {
                    if (ModelReceptor.ActivityID == Activities[i].ActivityID) // we need to compare with real Receptor value
                    {
                        foreach (SReceptorsStruct Receptor in SReceptors) // go though real S-receprots
                        {
                            if (Receptor.SName == ModelReceptor.SName) // we found required real receptor
                            {
                                if (!((Receptor.SExist == true) && (Receptor.SValue == ModelReceptor.SValue))) // logic comparation - inverted
                                {
                                    ActivityValue = false; // comparation failed -> Activity logic function = false -> not this Activity
                                }

                            }
                        }
                    }
                }
                if (ActivityValue == false)
                {

                    TempActivity = Activities[i];
                    TempActivity.ActivityValue = ActivityValue;
                    Activities[i] = TempActivity;
                }
                ActivityValue = true;
            }
        }

        //-------------------------------------------------------------
        private void Finalizator()
        {
            int ActivityMaxWeight = 0;
            for (int i = 0; i < Activities.Count; i++)
            {
                //Console.WriteLine("{0}\t{1}", Activities[i].ActivityName, Activities[i].ActivityValue);
                if (ActivityMaxWeight< Activities[i].ActivityWeight && Activities[i].ActivityValue) //confirmed activity
                {
                    Activity = Activities[i].ActivityName;
                    ActivityMaxWeight = Activities[i].ActivityWeight;
                }
            }

            // add activity to DB
            String query = "UPDATE SD SET Activity='" + Activity + "' WHERE WellUID='" + CurrentData.WellUid +
                           "' AND RecordUID=" + CurrentData.RecordUid + ";";
            using (conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(query, conn);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    conn.Close();
                }

            }

            // clear old data from DB
            query = "DELETE FROM SD WHERE WellUID='" + CurrentData.WellUid + 
                           "' AND NOT RecordUID=" + CurrentData.RecordUid + 
                           " AND NOT RecordUID=" + LastData.RecordUid + 
                           " AND NOT Activity='" + Activity +"';";
            using (conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(query, conn);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    conn.Close();
                }

            }

            conn.Close();
        }

        //-------------------------------------------------------------
    }

    //=================================================================
    class SurfaceData
    {
        public String WellUid;
        public Int64 RecordUid;
        public double Depth;
        public DateTime Time;
        public double HookLoad;
        public double SPP;
        public double BitDepth;
        public double RPM;
        public double Block;
        public String Activity;
    }

    //=================================================================

    struct SReceptorsStruct
    {
        public String SName;
        public bool SExist; //true is S was calculated, false if S is unknown
        public int SValue;
        public int ActivityID;
    }

    //=================================================================

    struct ActivitiesStruct
    {
        public String ActivityName { get; set; }
        public int ActivityID { get; set; }
        public bool ActivityValue { get; set; }
        public int ActivityWeight { get; set; }
    }

    //=================================================================
}
