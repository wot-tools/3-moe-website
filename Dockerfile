FROM ruby:2.4.0

RUN apt-get update -qq
RUN apt-get install -y build-essential libmariadbd-dev

ENV APP_HOME /app

RUN mkdir $APP_HOME
WORKDIR $APP_HOME

ADD Gemfile* $APP_HOME/
ADD Gemfile.lock* APP_HOME/

RUN bundle install
ADD . $APP_HOME