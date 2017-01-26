require 'test_helper'

class StatsControllerTest < ActionDispatch::IntegrationTest
  test "should get distribution" do
    get stats_distribution_url
    assert_response :success
  end

end
