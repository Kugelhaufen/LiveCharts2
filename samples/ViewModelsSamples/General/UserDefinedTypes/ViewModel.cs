﻿using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace ViewModelsSamples.General.UserDefinedTypes;

public partial class ViewModel : ObservableObject
{
    public ViewModel()
    {
        // you can configure a type globally // mark

        // Ideally you should call this when your application starts
        // in this case we have an array of the City class
        // we need to compare the Population property of every city in our array

        LiveCharts.Configure(config =>
            config
                .HasMap<City>((city, point) =>
                {
                    // in this lambda function we take an instance of the City class (see city parameter)
                    // and the point in the chart for that instance (see point parameter)
                    // LiveCharts will call this method for every instance of our City class array,
                    // now we need to populate the point coordinates from our City instance to our point

                    // in this case we will use the Population property as our primary value (normally the Y coordinate)
                    point.PrimaryValue = (float)city.Population;

                    // then the secondary value (normally the X coordinate)
                    // will be the index of the given dog class in our array
                    point.SecondaryValue = point.Context.Index;
                })

                // lets also set a mapper for the CityDensity class
                .HasMap<CityDensity>((cityDensity, point) =>
                {
                    // in this case we will use the Population property in the Y axis (primary)
                    // and the LandArea property in the X axis (secondary)
                    point.PrimaryValue = (float)cityDensity.Population;
                    point.SecondaryValue = (float)cityDensity.LandArea;
                })
            );
    }

    public ISeries[] Series { get; set; } =
    {
        new LineSeries<City>
        {
            Name = "Population",
            // you can also configure this series locally // mark
            //Mapping = (city, point) =>
            //{
            //    point.PrimaryValue = (float)city.Population;
            //    point.SecondaryValue = point.Context.Index;
            //},
            Values = new[]
            {
                new City { Name = "Tokyo", Population = 4 },
                new City { Name = "New York", Population = 6 },
                new City { Name = "Seoul", Population = 2 },
                new City { Name = "Moscow", Population = 8 },
                new City { Name = "Shanghai", Population = 3 },
                new City { Name = "Guadalajara", Population = 4 }
            }
        }
    };
}
