﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class SeriesSearchServiceFixture : CoreTest<SeriesSearchService>
    {
        private Series _series;

        [SetUp]
        public void Setup()
        {
            _series = new Series
                      {
                          Id = 1,
                          Title = "Title",
                          Seasons = new List<Season>()
                      };

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(It.IsAny<Int32>()))
                  .Returns(_series);

            Mocker.GetMock<ISearchForNzb>()
                  .Setup(s => s.SeasonSearch(_series.Id, It.IsAny<Int32>()))
                  .Returns(new List<DownloadDecision>());

            Mocker.GetMock<IProcessDownloadDecisions>()
                  .Setup(s => s.ProcessDecisions(It.IsAny<List<DownloadDecision>>()))
                  .Returns(new ProcessedDecisions(new List<DownloadDecision>(), new List<DownloadDecision>()));
        }

        [Test]
        public void should_only_include_monitored_seasons()
        {
            _series.Seasons = new List<Season>
                              {
                                  new Season { SeasonNumber = 0, Monitored = false },
                                  new Season { SeasonNumber = 1, Monitored = true }
                              };

            Subject.Execute(new SeriesSearchCommand{ SeriesId = _series.Id });

            Mocker.GetMock<ISearchForNzb>()
                .Verify(v => v.SeasonSearch(_series.Id, It.IsAny<Int32>()), Times.Exactly(_series.Seasons.Count(s => s.Monitored)));
        }

        [Test]
        public void should_start_with_lower_seasons_first()
        {
            var seasonOrder = new List<Int32>();

            _series.Seasons = new List<Season>
                              {
                                  new Season { SeasonNumber = 3, Monitored = true },
                                  new Season { SeasonNumber = 1, Monitored = true },
                                  new Season { SeasonNumber = 2, Monitored = true }
                              };

            Mocker.GetMock<ISearchForNzb>()
                  .Setup(s => s.SeasonSearch(_series.Id, It.IsAny<Int32>()))
                  .Returns(new List<DownloadDecision>())
                  .Callback<Int32, Int32>((seriesId, seasonNumber) => seasonOrder.Add(seasonNumber));

            Subject.Execute(new SeriesSearchCommand { SeriesId = _series.Id });

            seasonOrder.First().Should().Be(_series.Seasons.OrderBy(s => s.SeasonNumber).First().SeasonNumber);
        }
    }
}
