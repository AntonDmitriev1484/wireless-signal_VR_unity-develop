using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static CsvReader;

public class TEST_CsvReader : MonoBehaviour
{
    //  the CSV filename 
    private string csvFileName = "ray_path_data_Test_5cols.csv";
    //private string csvFileName = "paths_Living_Room_1st_floor_Tx_55_RxTen2Rows.csv";


    // List to store all the parsed row data
    private List<CsvReader.RayPathStruct> csvRaysData = new List<CsvReader.RayPathStruct>();

    // hold the CsvReader instance
    private CsvReader csvReader;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //----------------------------------------------
        Debug.Log("Attempting to read CSV data from file: " + csvFileName);

        // If your file is in a subfolder, e.g., Assets/Data/ray_path_data.csv,
        // use: Path.Combine(Application.dataPath, "Data", filename);
        string filePath = Path.Combine(Application.dataPath, "Data", csvFileName);
        Debug.Log("CSV FilePath: " + filePath); // Debugging line to check the file path

        if (!File.Exists(filePath))
        {
            Debug.LogError("CSV file not found at path: " + filePath);
            return;
        }

        // Create an instance of CsvReader
        csvReader = gameObject.AddComponent<CsvReader>();


        // call the ReadAndParseCsv method from CsvReader
        csvRaysData = csvReader.ReadAndParseCsv(filePath);

        // Now you can access the parsed data:
        csvReader.DisplayParsedData(csvRaysData);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
